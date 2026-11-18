namespace Foods.Application.Customer.Catalog
{
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData.Item;
    using Foods.Application.Abstraction.Query;
    using Foods.Application.Abstraction.Command;
    using Foods.Application.Abstraction.Security;

    /// <summary>One effective ingredient line of a dish for one customer (base line, changed line or added line).</summary>
    /// <summary>One part (صنف) of a dish for one customer.</summary>
    public sealed record EffectivePart(Guid ItemId, Guid PartItemId, bool IsOptional, bool Selected, bool IsAdded, int Sort);

    public sealed record EffectiveLine(Guid ItemId, Guid ContentId, Guid UnitId, decimal Quantity, bool IsAdded, bool IsChanged, decimal? BaseQuantity, bool IsOptional = false, bool IsDefault = true);

    /// <summary>
    /// "نسختك الخاصة": a customer's personal version of a dish. The dish lines and steps stay untouched;
    /// the customer's differences (UserItemContents / UserItemSteps) are applied on top, for that customer only.
    /// </summary>
    public static class PersonalRecipe
    {
        /// <summary>
        /// Effective lines of the dishes for the user: base lines minus removed, changed ones replaced, added ones appended.
        /// The base lines are the wanted country's own version (or the general one); the customer's edits are per country version.
        /// </summary>
        public static async Task<List<EffectiveLine>> Lines(IFoodsDbContext db, Guid userId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken, RecipeCountries? wanted = null)
        {
            if (itemIds.Count == 0) return new List<EffectiveLine>();
            wanted ??= await CountryVersions.Default(db, userId, cancellationToken);
            var rows = await CountryVersions.RowCountries(db, wanted, itemIds, cancellationToken);
            var baseLines = await CountryVersions.Contents(db, rows, cancellationToken);
            var mine = userId == Guid.Empty
                ? new List<UserItemContent>()
                : (await db.UserItemContents
                    .Where(e => e.UserId == userId && itemIds.Contains(e.ItemId) && !e.IsDeleted)
                    .ToListAsync(cancellationToken))
                    .Where(e => e.CountryId == wanted.For(e.ItemId)).ToList();
            return Merge(baseLines, mine);
        }

        /// <summary>The general or country lines the customer's version starts from.</summary>
        public static async Task<List<ItemContent>> BaseLines(IFoodsDbContext db, Guid itemId, Guid? country, CancellationToken cancellationToken)
        {
            var rows = await CountryVersions.RowCountries(db, RecipeCountries.Of(country, itemId), new[] { itemId }, cancellationToken);
            return await CountryVersions.Contents(db, rows, cancellationToken);
        }

        public static async Task<List<ItemPart>> BaseParts(IFoodsDbContext db, Guid itemId, Guid? country, CancellationToken cancellationToken)
        {
            var rows = await CountryVersions.RowCountries(db, RecipeCountries.Of(country, itemId), new[] { itemId }, cancellationToken);
            return await CountryVersions.Parts(db, rows, cancellationToken);
        }

        public static List<EffectiveLine> Merge(IReadOnlyList<ItemContent> baseLines, IReadOnlyList<UserItemContent> mine)
        {
            var result = new List<EffectiveLine>();
            var overridesByItem = mine.GroupBy(e => e.ItemId).ToDictionary(g => g.Key, g => g.GroupBy(e => e.ContentId).ToDictionary(c => c.Key, c => c.Last()));
            var itemIds = baseLines.Select(e => e.ItemId).Concat(mine.Select(e => e.ItemId)).Distinct();
            foreach (var itemId in itemIds)
            {
                overridesByItem.TryGetValue(itemId, out var overrides);
                overrides ??= new Dictionary<Guid, UserItemContent>();
                var used = new HashSet<Guid>();
                foreach (var line in baseLines.Where(e => e.ItemId == itemId))
                {
                    if (overrides.TryGetValue(line.ContentId, out var change))
                    {
                        if (!used.Add(line.ContentId) || change.IsRemoved) continue;
                        var changed = change.UnitId != line.UnitId || change.Quantity != line.Quantity;
                        result.Add(new EffectiveLine(itemId, line.ContentId, change.UnitId, change.Quantity, false, changed, line.Quantity, line.IsOptional, line.IsDefault));
                        continue;
                    }
                    result.Add(new EffectiveLine(itemId, line.ContentId, line.UnitId, line.Quantity, false, false, line.Quantity, line.IsOptional, line.IsDefault));
                }
                foreach (var added in overrides.Values.Where(e => !e.IsRemoved && !used.Contains(e.ContentId)
                             && !baseLines.Any(b => b.ItemId == itemId && b.ContentId == e.ContentId)))
                    result.Add(new EffectiveLine(itemId, added.ContentId, added.UnitId, added.Quantity, true, false, null));
            }
            return result;
        }

        /// <summary>The customer's choices for optional ingredients / parts of the wanted version: (ItemId, Kind, RefId) -> selected.</summary>
        public static async Task<Dictionary<(Guid ItemId, int Kind, Guid RefId), bool>> Choices(IFoodsDbContext db, Guid userId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken, RecipeCountries? wanted = null)
        {
            if (userId == Guid.Empty || itemIds.Count == 0) return new();
            wanted ??= await CountryVersions.Default(db, userId, cancellationToken);
            var rows = await db.UserItemChoices
                .Where(e => e.UserId == userId && itemIds.Contains(e.ItemId) && !e.IsDeleted)
                .ToListAsync(cancellationToken);
            var map = new Dictionary<(Guid, int, Guid), bool>();
            foreach (var row in rows.Where(e => e.CountryId == wanted.For(e.ItemId))) map[(row.ItemId, row.Kind, row.RefId)] = row.Selected;
            return map;
        }

        public static bool IsSelected(Dictionary<(Guid ItemId, int Kind, Guid RefId), bool> choices, Guid itemId, int kind, Guid refId, bool isOptional, bool isDefault)
            => !isOptional || (choices.TryGetValue((itemId, kind, refId), out var picked) ? picked : isDefault);

        /// <summary>
        /// The parts (أصناف) of the dishes for one customer: the admin's parts with the customer's pick
        /// (a required part can also be dropped in "تعديل الأكلة ليا"), plus dishes the customer added (IsAdded).
        /// </summary>
        public static async Task<List<EffectivePart>> Parts(IFoodsDbContext db, Guid userId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken, RecipeCountries? wanted = null)
        {
            if (itemIds.Count == 0) return new();
            wanted ??= await CountryVersions.Default(db, userId, cancellationToken);
            var choices = await Choices(db, userId, itemIds, cancellationToken, wanted);
            var rowCountries = await CountryVersions.RowCountries(db, wanted, itemIds, cancellationToken);
            var parts = await CountryVersions.Parts(db, rowCountries, cancellationToken);
            var result = new List<EffectivePart>();
            foreach (var part in parts)
            {
                var selected = choices.TryGetValue((part.ItemId, UserItemChoice.PartKind, part.PartItemId), out var picked)
                    ? picked
                    : !part.IsOptional || part.IsDefault;
                result.Add(new EffectivePart(part.ItemId, part.PartItemId, part.IsOptional, selected, false, part.Sort));
            }
            var sort = 1000;
            foreach (var choice in choices.Where(e => e.Key.Kind == UserItemChoice.PartKind && e.Value))
            {
                if (parts.Any(p => p.ItemId == choice.Key.ItemId && p.PartItemId == choice.Key.RefId)) continue;
                result.Add(new EffectivePart(choice.Key.ItemId, choice.Key.RefId, false, true, true, sort++));
            }
            var ids = result.Select(e => e.PartItemId).Distinct().ToList();
            var live = (await db.Items.Where(e => ids.Contains(e.Id) && !e.IsDeleted).Select(e => e.Id).ToListAsync(cancellationToken)).ToHashSet();
            return result.Where(e => live.Contains(e.PartItemId) && e.PartItemId != e.ItemId).ToList();
        }

        /// <summary>
        /// What really gets cooked/bought for the dishes: the customer's lines without the optional ingredients
        /// they did not pick, plus the lines of every included part (صنف), parts of parts too.
        /// Part lines are returned under the top dish's ItemId so orders keep one row per dish.
        /// A part is taken in the same country version as its top dish.
        /// </summary>
        public static async Task<List<EffectiveLine>> ForOrder(IFoodsDbContext db, Guid userId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken, RecipeCountries? wanted = null)
        {
            var result = new List<EffectiveLine>();
            if (itemIds.Count == 0) return result;
            wanted ??= await CountryVersions.Default(db, userId, cancellationToken);
            var frontier = itemIds.Distinct().Select(id => (Root: id, Item: id, Country: wanted.For(id))).ToList();
            for (var depth = 0; frontier.Count > 0 && depth <= Foods.Application.MasterData.Item.ItemPart.ItemParts.MaxDepth; depth++)
            {
                var next = new List<(Guid Root, Guid Item, Guid? Country)>();
                foreach (var group in frontier.GroupBy(e => e.Country))
                {
                    var ids = group.Select(e => e.Item).Distinct().ToList();
                    var scope = new RecipeCountries(group.Key);
                    var lines = await Lines(db, userId, ids, cancellationToken, scope);
                    var choices = await Choices(db, userId, ids, cancellationToken, scope);
                    var parts = await Parts(db, userId, ids, cancellationToken, scope);
                    foreach (var (root, item, country) in group)
                    {
                        foreach (var line in lines.Where(e => e.ItemId == item))
                        {
                            if (!IsSelected(choices, item, UserItemChoice.ContentKind, line.ContentId, line.IsOptional, line.IsDefault)) continue;
                            result.Add(line with { ItemId = root });
                        }
                        foreach (var part in parts.Where(e => e.ItemId == item && e.Selected))
                            next.Add((root, part.PartItemId, country));
                    }
                }
                frontier = next;
            }
            return result;
        }

        /// <summary>The user's steps (for that country version) when they have any, otherwise the version's steps.</summary>
        public static async Task<(List<ItemStepDto> Steps, bool Custom)> Steps(IFoodsDbContext db, Guid userId, Guid itemId, CancellationToken cancellationToken, Guid? country = null)
        {
            country ??= await CountryVersions.HomeCountry(db, userId, cancellationToken);
            var mine = userId == Guid.Empty
                ? new List<UserItemStep>()
                : await db.UserItemSteps.Where(e => e.UserId == userId && e.ItemId == itemId && !e.IsDeleted && e.CountryId == country).OrderBy(e => e.Sort).ToListAsync(cancellationToken);
            if (mine.Count > 0)
                return (mine.Select(e => new ItemStepDto(e.Sort, e.Body)).ToList(), true);
            var steps = await BaseSteps(db, itemId, country, cancellationToken);
            return (steps.Select(e => new ItemStepDto(e.Sort, e.Body)).ToList(), false);
        }

        public static async Task<List<ItemStep>> BaseSteps(IFoodsDbContext db, Guid itemId, Guid? country, CancellationToken cancellationToken)
        {
            var rows = await CountryVersions.RowCountries(db, RecipeCountries.Of(country, itemId), new[] { itemId }, cancellationToken);
            return await CountryVersions.Steps(db, itemId, rows[itemId], cancellationToken);
        }

        public static async Task<List<ItemVideo>> Videos(IFoodsDbContext db, Guid itemId, Guid? country, CancellationToken cancellationToken)
        {
            var rows = await CountryVersions.RowCountries(db, RecipeCountries.Of(country, itemId), new[] { itemId }, cancellationToken);
            return await CountryVersions.Videos(db, itemId, rows[itemId], cancellationToken);
        }

        public static async Task<bool> IsCustomized(IFoodsDbContext db, Guid userId, Guid itemId, CancellationToken cancellationToken, Guid? country = null)
        {
            if (userId == Guid.Empty) return false;
            country ??= await CountryVersions.HomeCountry(db, userId, cancellationToken);
            if (await db.UserItemContents.AnyAsync(e => e.UserId == userId && e.ItemId == itemId && !e.IsDeleted && e.CountryId == country, cancellationToken)
                || await db.UserItemSteps.AnyAsync(e => e.UserId == userId && e.ItemId == itemId && !e.IsDeleted && e.CountryId == country, cancellationToken))
                return true;
            // An added dish, or a required one the customer dropped (optional picks are normal choices).
            var optional = (await BaseParts(db, itemId, country, cancellationToken)).Where(e => e.IsOptional).Select(e => e.PartItemId).ToList();
            return await db.UserItemChoices.AnyAsync(e => e.UserId == userId && e.ItemId == itemId && !e.IsDeleted && e.CountryId == country
                && e.Kind == UserItemChoice.PartKind && !optional.Contains(e.RefId), cancellationToken);
        }

        /// <summary>Part rows for the client (names in the customer's dialect, photo, ingredient count).</summary>
        public static async Task<List<ItemPartView>> PartViews(IFoodsDbContext db, Guid userId, Guid itemId, MenuView view, bool includeDropped, CancellationToken cancellationToken, Guid? country = null)
        {
            country ??= await CountryVersions.HomeCountry(db, userId, cancellationToken);
            var scope = new RecipeCountries(country);
            var parts = (await Parts(db, userId, new[] { itemId }, cancellationToken, scope))
                .Where(e => includeDropped || e.Selected || e.IsOptional)
                .OrderBy(e => e.Sort).ToList();
            if (parts.Count == 0) return new();
            var ids = parts.Select(e => e.PartItemId).Distinct().ToList();
            var items = await db.Items.Where(e => ids.Contains(e.Id) && !e.IsDeleted).ToDictionaryAsync(e => e.Id, cancellationToken);
            var spoken = view.CountryId is Guid countryId
                ? await MenuDialect.ForItems(db, countryId, ids, cancellationToken)
                : new Dictionary<Guid, ItemCountry>();
            var counts = (await ForOrder(db, userId, ids, cancellationToken, scope))
                .GroupBy(e => e.ItemId).ToDictionary(g => g.Key, g => g.Select(e => e.ContentId).Distinct().Count());
            return parts.Where(e => items.ContainsKey(e.PartItemId)).Select(part =>
            {
                var dish = items[part.PartItemId];
                spoken.TryGetValue(dish.Id, out var local);
                return new ItemPartView(
                    dish.Id,
                    MenuDialect.Pick(local?.Name, dish.Name),
                    MenuDialect.Pick(local?.Summary, dish.Summary),
                    dish.ImagePath,
                    part.IsOptional,
                    part.Selected,
                    view.ShowAll || local != null,
                    counts.GetValueOrDefault(dish.Id),
                    part.IsAdded);
            }).ToList();
        }

        /// <summary>Ingredient rows for the client (names in the customer's dialect, units, places, photo album) in one batch.</summary>
        public static async Task<List<ItemIngredient>> Ingredients(IFoodsDbContext db, IReadOnlyList<EffectiveLine> lines, CancellationToken cancellationToken, Guid? nameCountry = null)
        {
            var contentIds = lines.Select(e => e.ContentId).Distinct().ToList();
            var contents = await db.Contents.Where(e => contentIds.Contains(e.Id)).ToListAsync(cancellationToken);
            var spoken = await CountryVersions.ContentsIn(db, nameCountry, contentIds, cancellationToken);
            var unitIds = lines.Select(e => e.UnitId).Distinct().ToList();
            var units = await db.Units.Where(e => unitIds.Contains(e.Id)).ToListAsync(cancellationToken);
            var placeIds = contents.Where(e => e.PurchasePlaceId != null).Select(e => e.PurchasePlaceId!.Value).Distinct().ToList();
            var places = await db.PurchasePlaces.Where(e => placeIds.Contains(e.Id)).ToListAsync(cancellationToken);
            var albums = await AlbumImages.ForContents(db, contents.Select(e => (e.Id, e.ImagePath)), cancellationToken);
            return lines.Select(line =>
            {
                var content = contents.FirstOrDefault(e => e.Id == line.ContentId);
                var place = content?.PurchasePlaceId == null ? null : places.FirstOrDefault(e => e.Id == content.PurchasePlaceId);
                return new ItemIngredient(
                    line.ContentId,
                    CountryVersions.Name(spoken, line.ContentId, content?.Name),
                    content?.ImagePath,
                    line.UnitId,
                    units.FirstOrDefault(e => e.Id == line.UnitId)?.Name,
                    line.Quantity,
                    content?.PurchasePlaceId,
                    place?.Name,
                    AlbumImages.Of(albums, line.ContentId, content?.ImagePath),
                    line.IsAdded,
                    line.IsChanged,
                    line.IsOptional,
                    !line.IsOptional || line.IsDefault);
            }).ToList();
        }
    }

    // ---------------- GET / PUT / DELETE customer/items/{id}/mine ----------------

    public sealed record MyItemRecipe(Guid ItemId, bool IsCustomized, bool StepsCustomized, List<ItemIngredient> Lines, List<ItemIngredient> Removed, List<ItemStepDto> Steps, List<ItemPartView>? Parts = null, Guid? CountryId = null, string? CountryName = null);
    public sealed record MyLineInput(Guid ContentId, Guid UnitId, decimal Quantity, bool Removed = false);
    public sealed record MyStepInput(int Sort, string? Body);

    public sealed record GetMyItemQuery(Guid ItemId, Guid? CountryId = null) : IQuery<MyItemRecipe>;
    /// <param name="Parts">Dishes that should be parts of the customer's version (null = keep as is).</param>
    public sealed record SaveMyItemCommand(Guid ItemId, IReadOnlyList<MyLineInput>? Lines, IReadOnlyList<MyStepInput>? Steps, IReadOnlyList<Guid>? Parts = null, Guid? CountryId = null) : ICommand<MyItemRecipe>;

    /// <summary>Dishes the customer can add as a part (hidden "part only" dishes included).</summary>
    public sealed record SearchPartPicksQuery(Guid ItemId, string? Text, int Take = 20) : IQuery<List<ItemPartView>>;

    public sealed class SearchPartPicksQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<SearchPartPicksQuery, List<ItemPartView>>
    {
        public async Task<Result<List<ItemPartView>>> Handle(SearchPartPicksQuery request, CancellationToken cancellationToken)
        {
            var text = (request.Text ?? "").Trim();
            if (text.Length > 100) text = text[..100];
            var take = Math.Clamp(request.Take, 1, 50);
            var view = await MenuDialect.For(db, currentUser.UserId, cancellationToken);
            var query = db.Items.Where(e => !e.IsDeleted && e.Id != request.ItemId);
            if (!view.ShowAll)
            {
                if (view.LiveId is not Guid countryId)
                    return new Result<List<ItemPartView>>(HttpStatusCode.OK, new List<ItemPartView>(), null);
                query = query.Where(item => db.ItemCountries.Any(link => link.ItemId == item.Id && link.CountryId == countryId && !link.IsDeleted));
            }
            if (text.Length > 0)
            {
                if (view.CountryId is Guid cid)
                    query = query.Where(e => (e.Name != null && e.Name.Contains(text))
                        || db.ItemCountries.Any(l => l.ItemId == e.Id && l.CountryId == cid && !l.IsDeleted && l.Name != null && l.Name.Contains(text)));
                else
                    query = query.Where(e => e.Name != null && e.Name.Contains(text));
            }
            var items = await query.OrderBy(e => e.Sort).ThenBy(e => e.Name).Take(take).ToListAsync(cancellationToken);
            var spoken = view.CountryId is Guid c2
                ? await MenuDialect.ForItems(db, c2, items.Select(e => e.Id), cancellationToken)
                : new Dictionary<Guid, ItemCountry>();
            var result = items.Select(e =>
            {
                spoken.TryGetValue(e.Id, out var local);
                return new ItemPartView(e.Id, MenuDialect.Pick(local?.Name, e.Name), MenuDialect.Pick(local?.Summary, e.Summary), e.ImagePath, false, true, view.ShowAll || local != null, 0, true);
            }).ToList();
            return new Result<List<ItemPartView>>(HttpStatusCode.OK, result, null);
        }
    }
    public sealed record ResetMyItemCommand(Guid ItemId, Guid? CountryId = null) : ICommand<MyItemRecipe>;

    file static class MyItem
    {
        public static readonly List<string> NotFound = new() { "الأكلة غير موجودة" };

        public static async Task<MyItemRecipe> Load(IFoodsDbContext db, Guid userId, Guid itemId, Guid? country, CancellationToken ct)
        {
            var view = await MenuDialect.For(db, userId, ct);
            var lines = await PersonalRecipe.Lines(db, userId, new[] { itemId }, ct, RecipeCountries.Of(country, itemId));
            var baseLines = await PersonalRecipe.BaseLines(db, itemId, country, ct);
            var kept = lines.Select(e => e.ContentId).ToHashSet();
            var removed = baseLines.Where(e => !kept.Contains(e.ContentId)).GroupBy(e => e.ContentId).Select(g => g.First())
                .Select(e => new EffectiveLine(itemId, e.ContentId, e.UnitId, e.Quantity, false, false, e.Quantity)).ToList();
            var (steps, stepsCustom) = await PersonalRecipe.Steps(db, userId, itemId, ct, country);
            return new MyItemRecipe(
                itemId,
                await PersonalRecipe.IsCustomized(db, userId, itemId, ct, country),
                stepsCustom,
                await PersonalRecipe.Ingredients(db, lines, ct, view.CountryId),
                await PersonalRecipe.Ingredients(db, removed, ct, view.CountryId),
                steps,
                await PersonalRecipe.PartViews(db, userId, itemId, view, true, ct, country),
                country,
                await CountryVersions.CountryName(db, country, ct));
        }
    }

    public sealed class GetMyItemQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<GetMyItemQuery, MyItemRecipe>
    {
        public async Task<Result<MyItemRecipe>> Handle(GetMyItemQuery request, CancellationToken cancellationToken)
        {
            if (!await db.Items.AnyAsync(e => e.Id == request.ItemId && !e.IsDeleted, cancellationToken))
                return new Result<MyItemRecipe>(HttpStatusCode.NotFound, null, MyItem.NotFound);
            var country = await CountryVersions.Check(db, currentUser.UserId, request.CountryId, cancellationToken);
            return new Result<MyItemRecipe>(HttpStatusCode.OK, await MyItem.Load(db, currentUser.UserId, request.ItemId, country, cancellationToken), null);
        }
    }

    public sealed class SaveMyItemCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<SaveMyItemCommand, MyItemRecipe>
    {
        private const int MaxLines = 80;
        private const int MaxSteps = 60;
        private const int MaxStepLength = 2000;

        public async Task<Result<MyItemRecipe>> Handle(SaveMyItemCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId;
            if (userId == Guid.Empty)
                return new Result<MyItemRecipe>(HttpStatusCode.Unauthorized, null, new List<string> { "سجل دخول الأول" });
            if (!await db.Items.AnyAsync(e => e.Id == request.ItemId && !e.IsDeleted, cancellationToken))
                return new Result<MyItemRecipe>(HttpStatusCode.NotFound, null, MyItem.NotFound);
            // The edit belongs to the country version the customer is looking at.
            var country = await CountryVersions.Check(db, userId, request.CountryId, cancellationToken);

            // Desired effective list (removed lines can also be omitted).
            var wanted = (request.Lines ?? Array.Empty<MyLineInput>()).Where(e => !e.Removed).ToList();
            if (wanted.Count > MaxLines)
                return Bad("عدد المكونات كبير قوي");
            if (wanted.Select(e => e.ContentId).Distinct().Count() != wanted.Count)
                return Bad("المكوّن متكرر");
            if (wanted.Any(e => e.Quantity <= 0 || e.Quantity > 100000))
                return Bad("الكمية لازم تكون أكبر من صفر");
            var contentIds = wanted.Select(e => e.ContentId).ToList();
            var validContents = await db.Contents.Where(e => contentIds.Contains(e.Id) && !e.IsDeleted).Select(e => e.Id).ToListAsync(cancellationToken);
            if (validContents.Count != contentIds.Count)
                return Bad("اختار من المكونات الموجودة بس");
            var unitIds = wanted.Select(e => e.UnitId).Distinct().ToList();
            var validUnits = await db.Units.Where(e => unitIds.Contains(e.Id) && !e.IsDeleted).CountAsync(cancellationToken);
            if (validUnits != unitIds.Count)
                return Bad("الوحدة غير موجودة");

            var steps = request.Steps?.OrderBy(e => e.Sort).Select(e => (e.Body ?? "").Trim()).Where(e => e.Length > 0).ToList();
            if (steps != null && (steps.Count > MaxSteps || steps.Any(e => e.Length > MaxStepLength)))
                return Bad("الخطوات كتير أو طويلة قوي");

            // Store only the differences from the dish.
            var baseLines = (await PersonalRecipe.BaseLines(db, request.ItemId, country, cancellationToken))
                .GroupBy(e => e.ContentId).Select(g => g.First()).ToList();
            if (request.Lines != null)
            {
            var old = await db.UserItemContents.Where(e => e.UserId == userId && e.ItemId == request.ItemId && !e.IsDeleted && e.CountryId == country).ToListAsync(cancellationToken);
            foreach (var row in old) row.IsDeleted = true;

            var wantedById = wanted.ToDictionary(e => e.ContentId);
            foreach (var line in baseLines)
            {
                if (!wantedById.TryGetValue(line.ContentId, out var want))
                    Add(userId, request.ItemId, country, line.ContentId, line.UnitId, 0, removed: true);
                else if (want.UnitId != line.UnitId || want.Quantity != line.Quantity)
                    Add(userId, request.ItemId, country, want.ContentId, want.UnitId, want.Quantity, removed: false);
            }
            var baseIds = baseLines.Select(e => e.ContentId).ToHashSet();
            foreach (var want in wanted.Where(e => !baseIds.Contains(e.ContentId)))
                Add(userId, request.ItemId, country, want.ContentId, want.UnitId, want.Quantity, removed: false);
            }

            if (request.Parts != null)
            {
                var wantedParts = request.Parts.Where(e => e != Guid.Empty && e != request.ItemId).Distinct().ToList();
                if (wantedParts.Count > 30)
                    return Bad("عدد الأصناف كبير قوي");
                var found = await db.Items.CountAsync(e => wantedParts.Contains(e.Id) && !e.IsDeleted, cancellationToken);
                if (found != wantedParts.Count)
                    return Bad("اختار من الأكلات الموجودة بس");
                foreach (var partId in wantedParts)
                {
                    if (await Foods.Application.MasterData.Item.ItemPart.ItemParts.MakesLoop(db, request.ItemId, partId, cancellationToken))
                        return Bad("الأكلة دي فيها الأكلة الأصلية كصنف، فمينفعش تتضاف");
                }
                var baseParts = await PersonalRecipe.BaseParts(db, request.ItemId, country, cancellationToken);
                var oldChoices = await db.UserItemChoices.Where(e => e.UserId == userId && e.ItemId == request.ItemId && e.Kind == UserItemChoice.PartKind && !e.IsDeleted && e.CountryId == country).ToListAsync(cancellationToken);
                foreach (var row in oldChoices) row.IsDeleted = true;
                void Choose(Guid refId, bool selected) => db.UserItemChoices.Add(new UserItemChoice
                {
                    Id = Guid.NewGuid(), UserId = userId, ItemId = request.ItemId, Kind = UserItemChoice.PartKind,
                    RefId = refId, Selected = selected, CountryId = country, IsDeleted = false, Status = 0
                });
                // Only differences from the dish are stored.
                foreach (var part in baseParts.GroupBy(e => e.PartItemId).Select(g => g.First()))
                {
                    var want = wantedParts.Contains(part.PartItemId);
                    if (want != (!part.IsOptional || part.IsDefault)) Choose(part.PartItemId, want);
                }
                var baseIds = baseParts.Select(e => e.PartItemId).ToHashSet();
                foreach (var partId in wantedParts.Where(e => !baseIds.Contains(e)))
                    Choose(partId, true);
            }

            if (steps != null)
            {
                var oldSteps = await db.UserItemSteps.Where(e => e.UserId == userId && e.ItemId == request.ItemId && !e.IsDeleted && e.CountryId == country).ToListAsync(cancellationToken);
                foreach (var row in oldSteps) row.IsDeleted = true;
                var baseSteps = (await PersonalRecipe.BaseSteps(db, request.ItemId, country, cancellationToken)).Select(e => e.Body ?? "").ToList();
                var same = baseSteps.Select(e => e.Trim()).Where(e => e.Length > 0).SequenceEqual(steps);
                if (!same && steps.Count > 0)
                {
                    for (var i = 0; i < steps.Count; i++)
                        db.UserItemSteps.Add(new UserItemStep { Id = Guid.NewGuid(), UserId = userId, ItemId = request.ItemId, Sort = i + 1, Body = steps[i], CountryId = country, IsDeleted = false, Status = 0 });
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            return new Result<MyItemRecipe>(HttpStatusCode.OK, await MyItem.Load(db, userId, request.ItemId, country, cancellationToken), null);
        }

        private void Add(Guid userId, Guid itemId, Guid? country, Guid contentId, Guid unitId, decimal quantity, bool removed)
            => db.UserItemContents.Add(new UserItemContent
            {
                Id = Guid.NewGuid(), UserId = userId, ItemId = itemId, CountryId = country, ContentId = contentId, UnitId = unitId,
                Quantity = quantity, IsRemoved = removed, IsDeleted = false, Status = 0
            });

        private static Result<MyItemRecipe> Bad(string message)
            => new(HttpStatusCode.BadRequest, null, new List<string> { message });
    }

    public sealed class ResetMyItemCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<ResetMyItemCommand, MyItemRecipe>
    {
        public async Task<Result<MyItemRecipe>> Handle(ResetMyItemCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId;
            if (!await db.Items.AnyAsync(e => e.Id == request.ItemId && !e.IsDeleted, cancellationToken))
                return new Result<MyItemRecipe>(HttpStatusCode.NotFound, null, MyItem.NotFound);
            var country = await CountryVersions.Check(db, userId, request.CountryId, cancellationToken);
            var lines = await db.UserItemContents.Where(e => e.UserId == userId && e.ItemId == request.ItemId && !e.IsDeleted && e.CountryId == country).ToListAsync(cancellationToken);
            var steps = await db.UserItemSteps.Where(e => e.UserId == userId && e.ItemId == request.ItemId && !e.IsDeleted && e.CountryId == country).ToListAsync(cancellationToken);
            foreach (var row in lines) row.IsDeleted = true;
            foreach (var row in steps) row.IsDeleted = true;
            var choices = await db.UserItemChoices.Where(e => e.UserId == userId && e.ItemId == request.ItemId && !e.IsDeleted && e.CountryId == country).ToListAsync(cancellationToken);
            foreach (var row in choices) row.IsDeleted = true;
            await db.SaveChangesAsync(cancellationToken);
            return new Result<MyItemRecipe>(HttpStatusCode.OK, await MyItem.Load(db, userId, request.ItemId, country, cancellationToken), null);
        }
    }

    // ---------------- ingredient picker: GET customer/contents ----------------

    public sealed record ContentPick(Guid Id, string? Name, string? ImagePath, Guid UnitId, string? UnitName, decimal MiniQuantity);
    public sealed record UnitPick(Guid Id, string? Name);
    public sealed record ContentPickResult(List<ContentPick> Items, List<UnitPick> Units);
    public sealed record SearchContentsQuery(string? Text, int Take = 20) : IQuery<ContentPickResult>;

    public sealed class SearchContentsQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<SearchContentsQuery, ContentPickResult>
    {
        public async Task<Result<ContentPickResult>> Handle(SearchContentsQuery request, CancellationToken cancellationToken)
        {
            var text = (request.Text ?? "").Trim();
            if (text.Length > 100) text = text[..100];
            var take = Math.Clamp(request.Take, 1, 50);
            var view = await MenuDialect.For(db, currentUser.UserId, cancellationToken);
            var query = db.Contents.Where(e => !e.IsDeleted);
            if (text.Length > 0)
            {
                // Search the general name and the name in the customer's dialect.
                if (view.CountryId is Guid cid)
                    query = query.Where(e => (e.Name != null && e.Name.Contains(text))
                        || db.ContentCountries.Any(l => l.ContentId == e.Id && l.CountryId == cid && !l.IsDeleted && l.Name != null && l.Name.Contains(text)));
                else
                    query = query.Where(e => e.Name != null && e.Name.Contains(text));
            }
            // Ingredients of the country the customer is in first, then its neighbors.
            var (home, near) = await CuisineOrder.Ranked(db, view, cancellationToken);
            var rows = await query
                .OrderBy(e => e.CuisineId != null && home.Contains(e.CuisineId.Value) ? 0 : e.CuisineId != null && near.Contains(e.CuisineId.Value) ? 1 : 2)
                .ThenBy(e => e.Sort).ThenBy(e => e.Name).Take(take)
                .Select(e => new { e.Id, e.Name, e.ImagePath, e.UnitId, e.MiniQuantity }).ToListAsync(cancellationToken);
            var spoken = await CountryVersions.ContentsIn(db, view.CountryId, rows.Select(e => e.Id), cancellationToken);
            var units = await db.Units.Where(e => !e.IsDeleted).OrderBy(e => e.Name).Select(e => new UnitPick(e.Id, e.Name)).ToListAsync(cancellationToken);
            var names = units.ToDictionary(e => e.Id, e => e.Name);
            return new Result<ContentPickResult>(HttpStatusCode.OK, new ContentPickResult(
                rows.Select(e => new ContentPick(e.Id, CountryVersions.Name(spoken, e.Id, e.Name), e.ImagePath, e.UnitId, names.GetValueOrDefault(e.UnitId), e.MiniQuantity)).ToList(),
                units), null);
        }
    }
}
