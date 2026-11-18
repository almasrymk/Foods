namespace Foods.Application.Customer.Pricing
{
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using Foods.Domain.Entity.MasterData;
    using Foods.Domain.Entity.MasterData.Content;
    using Foods.Application.Abstraction.Query;
    using Foods.Application.Abstraction.Command;
    using Foods.Application.Abstraction.Security;
    using Foods.Application.Customer.Ordering;
    using Foods.Application.Customer.Catalog;

    /// <summary>The price used for one ingredient: Price (in the customer's currency) for Quantity of UnitId (the customer's own one when IsMine).</summary>
    public sealed record PriceInfo(decimal Price, decimal Quantity, Guid UnitId, string? UnitName, bool IsMine);

    /// <summary>
    /// Ingredient prices for one customer: their own price (أسعار المكونات) or else the admin price.
    /// Null from <see cref="For"/> when the customer did not turn prices on in settings.
    /// </summary>
    public sealed class PriceBook
    {
        private readonly IFoodsDbContext db;
        private readonly Guid userId;
        private readonly Dictionary<Guid, Unit> units;
        private readonly Dictionary<Guid, (PriceInfo? Info, decimal? PerBasic)> prices = new();
        public decimal Household { get; }
        /// <summary>The country the customer lives in (settings) and its currency: prices are that country's.</summary>
        public Guid? CountryId { get; }
        public string Currency { get; }

        private PriceBook(IFoodsDbContext db, Guid userId, Dictionary<Guid, Unit> units, decimal household, Guid? countryId, string currency)
        {
            this.db = db; this.userId = userId; this.units = units; Household = household; CountryId = countryId; Currency = currency;
        }

        /// <summary>The general admin price is in the default currency, so it only counts there.</summary>
        public bool UsesGeneral => Foods.Application.Customer.Catalog.CountryVersions.UsesGeneralPrice(Currency);

        public static async Task<bool> IsOn(IFoodsDbContext db, Guid userId, CancellationToken ct)
            => userId != Guid.Empty && await db.Set<UserSetting>().AnyAsync(e => e.UserId == userId && !e.IsDeleted && e.UsePrices, ct);

        public static async Task<PriceBook?> For(IFoodsDbContext db, Guid userId, CancellationToken ct, bool force = false)
        {
            if (!force && !await IsOn(db, userId, ct)) return null;
            var units = await db.Units.ToDictionaryAsync(e => e.Id, ct);
            var family = await db.Set<FamilyMember>().Where(e => e.UserId == userId && !e.IsDeleted).ToListAsync(ct);
            var bands = await db.AgeBands.Where(e => !e.IsDeleted).ToListAsync(ct);
            var (countryId, currency) = await Foods.Application.Customer.Catalog.CountryVersions.PriceCountry(db, userId, ct);
            return new PriceBook(db, userId, units, OrderCalculator.HouseholdFactor(family, bands), countryId, currency);
        }

        /// <summary>
        /// Weight / volume units (جرام، ملليلتر and the ones built on them: ملعقة، كوب، لتر، كيلو) convert into each other by BasicQuantity.
        /// Count units (حبة، فص، رأس، ربطة، عود، رغيف…) have no size, so they only match themselves:
        /// a cup of parsley can't be priced from a price per bunch.
        /// </summary>
        public bool Measured(Guid? unitId)
        {
            if (unitId is not Guid id || !units.TryGetValue(id, out var unit)) return false;
            if (unit.BasicQuantity > 1) return true;
            var name = (unit.Name ?? "").Trim();
            var nick = (unit.NickName ?? "").Trim();
            return name is "جرام" or "جم" or "ملليلتر" or "مل" or "ملي" or "ملل" || nick is "جم" or "مل" or "g" or "ml";
        }

        /// <summary>A recipe unit can be priced from the price unit: the same unit, or both weight / volume.</summary>
        public bool Convertible(Guid? from, Guid? to) => from == to || (Measured(from) && Measured(to));

        public decimal Basic(Guid? unitId) =>
            unitId is Guid id && units.TryGetValue(id, out var unit) && unit.BasicQuantity > 0 ? unit.BasicQuantity : 1m;

        public string? UnitName(Guid? unitId) => unitId is Guid id && units.TryGetValue(id, out var unit) ? unit.Name : null;

        /// <summary>Loads the prices of these ingredients (once each).</summary>
        public async Task Load(IEnumerable<Guid> contentIds, CancellationToken ct)
        {
            var ids = contentIds.Distinct().Where(e => !prices.ContainsKey(e)).ToList();
            if (ids.Count == 0) return;
            var contents = await db.Contents.Where(e => ids.Contains(e.Id)).ToListAsync(ct);
            var country = CountryId;
            var mine = (await db.UserContentPrices
                    .Where(e => e.UserId == userId && ids.Contains(e.ContentId) && !e.IsDeleted && e.CountryId == country)
                    .ToListAsync(ct))
                .GroupBy(e => e.ContentId).ToDictionary(g => g.Key, g => g.Last());
            var local = await Foods.Application.Customer.Catalog.CountryVersions.ContentsIn(db, CountryId, ids, ct);
            foreach (var id in ids)
            {
                var content = contents.FirstOrDefault(e => e.Id == id);
                prices[id] = content == null ? (null, null) : Resolve(content, mine.GetValueOrDefault(id), local.GetValueOrDefault(id));
            }
        }

        /// <summary>The admin price for the customer's country: the country's own price, else the general one (default currency only).</summary>
        public PriceInfo? AdminInfo(Content content, ContentCountry? local)
        {
            if (local != null && local.Price > 0)
            {
                var unit = local.PriceUnitId ?? content.UnitId;
                return new PriceInfo(local.Price, local.PriceQuantity > 0 ? local.PriceQuantity : 1, unit, UnitName(unit), false);
            }
            if (!UsesGeneral) return null;
            if (content.Price > 0)
            {
                var unit = content.PriceUnitId ?? content.UnitId;
                return new PriceInfo(content.Price, content.PriceQuantity > 0 ? content.PriceQuantity : 1, unit, UnitName(unit), false);
            }
            if (content.PurchaseMode == PurchaseRules.MoneyThenKilo && content.PurchaseUnitPrice > 0)
                return new PriceInfo(content.PurchaseUnitPrice, PurchaseRules.Step(content.PurchaseMode, content.PurchaseStep), content.UnitId, UnitName(content.UnitId), false);
            return null;
        }

        private (PriceInfo?, decimal?) Resolve(Content content, UserContentPrice? mine, ContentCountry? local)
        {
            if (mine != null && mine.Price > 0)
            {
                var unit = mine.PriceUnitId ?? content.UnitId;
                var qty = mine.PriceQuantity > 0 ? mine.PriceQuantity : 1;
                return (new PriceInfo(mine.Price, qty, unit, UnitName(unit), true), mine.Price / (qty * Basic(unit)));
            }
            // The country's price, else the general one; old data: "سعر الـ 1/8 كيلو" of mode 4 is a price too.
            var admin = AdminInfo(content, local);
            return admin == null ? (null, null) : (admin, admin.Price / (admin.Quantity * Basic(admin.UnitId)));
        }

        public PriceInfo? Info(Guid contentId) => prices.TryGetValue(contentId, out var p) ? p.Info : null;

        /// <summary>The price per basic unit, only when the price's unit converts to the wanted one.</summary>
        private decimal? PerBasicFor(Guid contentId, Guid unitId)
        {
            if (!prices.TryGetValue(contentId, out var p) || p.PerBasic is not decimal perBasic || p.Info == null) return null;
            return Convertible(unitId, p.Info.UnitId) ? perBasic : null;
        }

        /// <summary>Money for a quantity of an ingredient, null when the ingredient has no price.</summary>
        public decimal? Cost(Guid contentId, Guid unitId, decimal quantity)
        {
            if (PerBasicFor(contentId, unitId) is not decimal perBasic) return null;
            return Round(quantity * Basic(unitId) * perBasic);
        }

        /// <summary>Money of a shopping line: a "by money" line (mode 3) is its own price.</summary>
        public decimal? LineCost(ShoppingLineResult line)
            => line.PurchaseMode == PurchaseRules.Money ? line.Quantity : Cost(line.ContentId, line.UnitId, line.Quantity);

        /// <summary>Money of one unit of the line (so the client can follow the + / - counter).</summary>
        public decimal? UnitCost(Guid contentId, Guid unitId, int mode)
        {
            if (mode == PurchaseRules.Money) return 1;
            if (PerBasicFor(contentId, unitId) is not decimal perBasic) return null;
            return Math.Round(Basic(unitId) * perBasic, 6);
        }

        public ShoppingLineResult Priced(ShoppingLineResult line)
            => line with { Price = LineCost(line), UnitPrice = UnitCost(line.ContentId, line.UnitId, line.PurchaseMode) };

        public static decimal Round(decimal value) => Math.Round(value, 2);

        /// <summary>Price of each dish for the family (its ingredients, its parts' ingredients, the picked optional ones).</summary>
        public async Task<Dictionary<Guid, DishPrice>> Dishes(IReadOnlyCollection<Guid> itemIds, CancellationToken ct, Foods.Application.Customer.Catalog.RecipeCountries? wanted = null)
        {
            var result = new Dictionary<Guid, DishPrice>();
            if (itemIds.Count == 0) return result;
            var lines = await Foods.Application.Customer.Catalog.PersonalRecipe.ForOrder(db, userId, itemIds, ct, wanted);
            await Load(lines.Select(e => e.ContentId), ct);
            foreach (var group in lines.GroupBy(e => e.ItemId))
            {
                decimal perPerson = 0; var missing = 0;
                foreach (var line in group)
                {
                    var cost = Cost(line.ContentId, line.UnitId, line.Quantity);
                    if (cost is decimal c) perPerson += c; else missing++;
                }
                result[group.Key] = new DishPrice(Round(perPerson * Household), Round(perPerson), Household, missing);
            }
            return result;
        }
    }

    /// <summary>Price for the family (Total) and for one person; Missing = ingredients with no price yet.</summary>
    public sealed record DishPrice(decimal Total, decimal PerPerson, decimal People, int Missing);

    // ---------------- customer/prices ----------------

    public sealed record ContentPriceRow(
        Guid ContentId, string? Name, string? ImagePath, Guid UnitId, string? UnitName,
        decimal? Price, decimal Quantity, Guid PriceUnitId, string? PriceUnitName, bool IsMine,
        decimal? AdminPrice, decimal AdminQuantity, Guid AdminUnitId, string? AdminUnitName,
        List<PriceUnitOption> Units,
        Guid? CuisineId = null, Guid? CategoryId = null, Guid? GroupId = null, Guid? PlaceId = null);

    /// <summary>One section of the prices screen's advanced search (المطبخ / التصنيف / المجموعة / مكان الشراء).</summary>
    /// <param name="Key">cuisine, category, group or place: the row field it filters on.</param>
    public sealed record PriceFilterSection(string Key, string Title, List<PriceUnitOption> Options);

    public sealed record PriceUnitOption(Guid Id, string? Name);

    public sealed record CustomerPricesDto(bool Enabled, List<ContentPriceRow> Items, string Currency = Foods.Application.Customer.Catalog.CountryVersions.DefaultCurrency, Guid? CountryId = null, string? CountryName = null, List<PriceFilterSection>? Filters = null);

    public sealed record GetMyPricesQuery(string? Text, Guid? ContentId = null) : IQuery<CustomerPricesDto>;
    public sealed record SetPricesEnabledCommand(bool Enabled) : ICommand<bool>;
    public sealed record SaveMyPriceCommand(Guid ContentId, decimal Price, decimal Quantity, Guid? UnitId) : ICommand<ContentPriceRow>;
    public sealed record RemoveMyPriceCommand(Guid ContentId) : ICommand<ContentPriceRow>;

    file static class PriceRows
    {
        public static async Task<List<ContentPriceRow>> Build(IFoodsDbContext db, Guid userId, List<Content> contents, CancellationToken ct)
        {
            var book = (await PriceBook.For(db, userId, ct, force: true))!;
            await book.Load(contents.Select(e => e.Id), ct);
            var country = book.CountryId;
            var mine = (await db.UserContentPrices
                    .Where(e => e.UserId == userId && !e.IsDeleted && e.CountryId == country)
                    .Select(e => e.ContentId).ToListAsync(ct)).ToHashSet();
            var local = await Foods.Application.Customer.Catalog.CountryVersions.ContentsIn(db, book.CountryId, contents.Select(e => e.Id), ct);
            var nameCountry = await Foods.Application.Customer.Catalog.CountryVersions.OwnCountry(db, userId, ct);
            var spoken = await Foods.Application.Customer.Catalog.CountryVersions.ContentsIn(db, nameCountry, contents.Select(e => e.Id), ct);
            var allUnits = await db.Units.Where(e => !e.IsDeleted).OrderBy(e => e.BasicQuantity).ThenBy(e => e.Name).ToListAsync(ct);
            return contents.Select(content =>
            {
                var info = book.Info(content.Id);
                var admin = book.AdminInfo(content, local.GetValueOrDefault(content.Id));
                // Units the price can be written in: those with the same base (جرام / كوب / ملعقة...).
                var options = allUnits.Select(u => new PriceUnitOption(u.Id, u.Name)).ToList();
                return new ContentPriceRow(
                    content.Id, Foods.Application.Customer.Catalog.CountryVersions.Name(spoken, content.Id, content.Name), content.ImagePath, content.UnitId, book.UnitName(content.UnitId),
                    info?.Price, info?.Quantity ?? 1, info?.UnitId ?? content.UnitId, info?.UnitName ?? book.UnitName(content.UnitId),
                    info?.IsMine == true && mine.Contains(content.Id),
                    admin?.Price, admin?.Quantity ?? 1, admin?.UnitId ?? content.UnitId, admin?.UnitName ?? book.UnitName(content.UnitId),
                    options,
                    content.CuisineId, content.ContentCategoryId, content.ContentGroupId, content.PurchasePlaceId);
            }).ToList();
        }

        /// <summary>The advanced-search sections: only the values the listed ingredients use, in the admin order.</summary>
        public static async Task<List<PriceFilterSection>> Filters(IFoodsDbContext db, List<Content> contents, CancellationToken ct)
        {
            var cuisineIds = contents.Where(e => e.CuisineId != null).Select(e => e.CuisineId!.Value).Distinct().ToList();
            var categoryIds = contents.Select(e => e.ContentCategoryId).Distinct().ToList();
            var groupIds = contents.Select(e => e.ContentGroupId).Distinct().ToList();
            var placeIds = contents.Where(e => e.PurchasePlaceId != null).Select(e => e.PurchasePlaceId!.Value).Distinct().ToList();
            var cuisines = await db.Cuisines.Where(e => cuisineIds.Contains(e.Id) && !e.IsDeleted).OrderBy(e => e.Sort).ThenBy(e => e.Name).Select(e => new PriceUnitOption(e.Id, e.Name)).ToListAsync(ct);
            var categories = await db.ContentCategories.Where(e => categoryIds.Contains(e.Id) && !e.IsDeleted).OrderBy(e => e.Name).Select(e => new PriceUnitOption(e.Id, e.Name)).ToListAsync(ct);
            var groups = await db.ContentGroup.Where(e => groupIds.Contains(e.Id) && !e.IsDeleted).OrderBy(e => e.Name).Select(e => new PriceUnitOption(e.Id, e.Name)).ToListAsync(ct);
            var places = await db.PurchasePlaces.Where(e => placeIds.Contains(e.Id) && !e.IsDeleted).OrderBy(e => e.Sort).ThenBy(e => e.Name).Select(e => new PriceUnitOption(e.Id, e.Name)).ToListAsync(ct);
            var result = new List<PriceFilterSection>();
            if (cuisines.Count > 0) result.Add(new("cuisine", "المطبخ", cuisines));
            if (categories.Count > 0) result.Add(new("category", "التصنيف", categories));
            if (groups.Count > 0) result.Add(new("group", "المجموعة", groups));
            if (places.Count > 0) result.Add(new("place", "مكان الشراء", places));
            return result;
        }

    }

    public sealed class GetMyPricesQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<GetMyPricesQuery, CustomerPricesDto>
    {
        public async Task<Result<CustomerPricesDto>> Handle(GetMyPricesQuery request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId;
            var query = db.Contents.Where(e => !e.IsDeleted);
            if (request.ContentId is Guid id) query = query.Where(e => e.Id == id);
            var text = (request.Text ?? "").Trim();
            var own = await Foods.Application.Customer.Catalog.CountryVersions.OwnCountry(db, userId, cancellationToken);
            if (text.Length > 0)
                query = query.Where(e => (e.Name != null && e.Name.Contains(text))
                    || (own != null && db.ContentCountries.Any(l => l.ContentId == e.Id && l.CountryId == own && !l.IsDeleted && l.Name != null && l.Name.Contains(text))));
            var view = await MenuDialect.For(db, userId, cancellationToken);
            var (home, near) = await CuisineOrder.Ranked(db, view, cancellationToken);
            var contents = await query
                .OrderBy(e => e.CuisineId != null && home.Contains(e.CuisineId.Value) ? 0 : e.CuisineId != null && near.Contains(e.CuisineId.Value) ? 1 : 2)
                .ThenBy(e => e.Sort).ThenBy(e => e.Name).ToListAsync(cancellationToken);
            var rows = await PriceRows.Build(db, userId, contents, cancellationToken);
            // The filter sections come with the full list only (not for one ingredient).
            var filters = request.ContentId == null ? await PriceRows.Filters(db, contents, cancellationToken) : null;
            var enabled = await PriceBook.IsOn(db, userId, cancellationToken);
            var (countryId, currency) = await Foods.Application.Customer.Catalog.CountryVersions.PriceCountry(db, userId, cancellationToken);
            return new Result<CustomerPricesDto>(HttpStatusCode.OK, new CustomerPricesDto(enabled, rows, currency, countryId,
                await Foods.Application.Customer.Catalog.CountryVersions.CountryName(db, countryId, cancellationToken), filters), null);
        }
    }

    public sealed class SetPricesEnabledCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<SetPricesEnabledCommand, bool>
    {
        public async Task<Result<bool>> Handle(SetPricesEnabledCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId;
            if (userId == Guid.Empty)
                return new Result<bool>(HttpStatusCode.Unauthorized, false, new List<string> { "سجل دخول الأول" });
            var row = await db.Set<UserSetting>().FirstOrDefaultAsync(e => e.UserId == userId && !e.IsDeleted, cancellationToken);
            if (row == null)
            {
                row = new UserSetting { Id = Guid.NewGuid(), UserId = userId };
                db.Set<UserSetting>().Add(row);
            }
            row.UsePrices = request.Enabled;
            await db.SaveChangesAsync(cancellationToken);
            return new Result<bool>(HttpStatusCode.OK, request.Enabled, null);
        }
    }

    public sealed class SaveMyPriceCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<SaveMyPriceCommand, ContentPriceRow>
    {
        public async Task<Result<ContentPriceRow>> Handle(SaveMyPriceCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId;
            if (userId == Guid.Empty)
                return new Result<ContentPriceRow>(HttpStatusCode.Unauthorized, null, new List<string> { "سجل دخول الأول" });
            if (request.Price <= 0)
                return new Result<ContentPriceRow>(HttpStatusCode.BadRequest, null, new List<string> { "اكتب السعر" });
            var content = await db.Contents.FirstOrDefaultAsync(e => e.Id == request.ContentId && !e.IsDeleted, cancellationToken);
            if (content == null)
                return new Result<ContentPriceRow>(HttpStatusCode.NotFound, null, new List<string> { "المكوّن غير موجود" });
            var (country, _) = await Foods.Application.Customer.Catalog.CountryVersions.PriceCountry(db, userId, cancellationToken);
            var old = await db.UserContentPrices.Where(e => e.UserId == userId && e.ContentId == content.Id && !e.IsDeleted && e.CountryId == country).ToListAsync(cancellationToken);
            foreach (var row in old) row.IsDeleted = true;
            db.UserContentPrices.Add(new UserContentPrice
            {
                Id = Guid.NewGuid(), UserId = userId, ContentId = content.Id, CountryId = country,
                Price = request.Price, PriceQuantity = request.Quantity > 0 ? request.Quantity : 1,
                PriceUnitId = request.UnitId ?? content.UnitId, IsDeleted = false, Status = 0
            });
            await db.SaveChangesAsync(cancellationToken);
            var rows = await PriceRows.Build(db, userId, new List<Content> { content }, cancellationToken);
            return new Result<ContentPriceRow>(HttpStatusCode.OK, rows[0], null);
        }
    }

    public sealed class RemoveMyPriceCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<RemoveMyPriceCommand, ContentPriceRow>
    {
        public async Task<Result<ContentPriceRow>> Handle(RemoveMyPriceCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId;
            var content = await db.Contents.FirstOrDefaultAsync(e => e.Id == request.ContentId && !e.IsDeleted, cancellationToken);
            if (content == null)
                return new Result<ContentPriceRow>(HttpStatusCode.NotFound, null, new List<string> { "المكوّن غير موجود" });
            var (country, _) = await Foods.Application.Customer.Catalog.CountryVersions.PriceCountry(db, userId, cancellationToken);
            var old = await db.UserContentPrices.Where(e => e.UserId == userId && e.ContentId == content.Id && !e.IsDeleted && e.CountryId == country).ToListAsync(cancellationToken);
            foreach (var row in old) row.IsDeleted = true;
            await db.SaveChangesAsync(cancellationToken);
            var rows = await PriceRows.Build(db, userId, new List<Content> { content }, cancellationToken);
            return new Result<ContentPriceRow>(HttpStatusCode.OK, rows[0], null);
        }
    }
}

namespace Foods.Application.Customer.Pricing
{
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using Foods.Application.Abstraction.Query;
    using Foods.Application.Abstraction.Command;
    using Foods.Application.Abstraction.Security;
    using Foods.Application.Customer.Catalog;

    // ---------------- customer/prices/country: "البلد اللي أنت فيها" (prices and currency) ----------------

    public sealed record CountryOption(Guid Id, string? Name, string Currency);

    /// <param name="IsDefault">True = no country picked, so it follows the customer's own country.</param>
    public sealed record PriceCountryDto(Guid? CountryId, string? CountryName, string Currency, Guid? OwnCountryId, bool IsDefault, List<CountryOption> Countries);

    public sealed record GetPriceCountryQuery() : IQuery<PriceCountryDto>;
    /// <param name="CountryId">Null = back to the customer's own country.</param>
    public sealed record SetPriceCountryCommand(Guid? CountryId) : ICommand<PriceCountryDto>;

    file static class PriceCountries
    {
        public static async Task<PriceCountryDto> Load(IFoodsDbContext db, Guid userId, CancellationToken ct)
        {
            var (countryId, currency) = await CountryVersions.PriceCountry(db, userId, ct);
            var own = await CountryVersions.OwnCountry(db, userId, ct);
            var chosen = await db.Set<UserSetting>().Where(e => e.UserId == userId && !e.IsDeleted).Select(e => e.PriceCountryId).FirstOrDefaultAsync(ct);
            // The customer's own country first, then its neighbors.
            var ownRow = own is Guid o ? await db.Countries.FirstOrDefaultAsync(e => e.Id == o, ct) : null;
            var near = MenuDialect.ParseIds(ownRow?.Neighbors);
            var countries = (await db.Countries.Where(e => !e.IsDeleted).OrderBy(e => e.Name).ToListAsync(ct))
                .OrderBy(e => e.Id == own ? 0 : near.Contains(e.Id) ? 1 : 2)
                .Select(e => new CountryOption(e.Id, e.Name, string.IsNullOrWhiteSpace(e.Currency) ? CountryVersions.DefaultCurrency : e.Currency.Trim()))
                .ToList();
            return new PriceCountryDto(countryId, countries.FirstOrDefault(e => e.Id == countryId)?.Name, currency, own, chosen == null, countries);
        }
    }

    public sealed class GetPriceCountryQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<GetPriceCountryQuery, PriceCountryDto>
    {
        public async Task<Result<PriceCountryDto>> Handle(GetPriceCountryQuery request, CancellationToken cancellationToken)
            => new(HttpStatusCode.OK, await PriceCountries.Load(db, currentUser.UserId, cancellationToken), null);
    }

    public sealed class SetPriceCountryCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<SetPriceCountryCommand, PriceCountryDto>
    {
        public async Task<Result<PriceCountryDto>> Handle(SetPriceCountryCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId;
            if (userId == Guid.Empty)
                return new Result<PriceCountryDto>(HttpStatusCode.Unauthorized, null, new List<string> { "سجل دخول الأول" });
            var target = request.CountryId is Guid id && id != Guid.Empty ? id : (Guid?)null;
            if (target != null && !await db.Countries.AnyAsync(e => e.Id == target && !e.IsDeleted, cancellationToken))
                return new Result<PriceCountryDto>(HttpStatusCode.BadRequest, null, new List<string> { "البلد غير موجودة" });
            // Picking the own country again means "follow my country" (the default).
            if (target != null && target == await CountryVersions.OwnCountry(db, userId, cancellationToken)) target = null;
            var row = await db.Set<UserSetting>().FirstOrDefaultAsync(e => e.UserId == userId && !e.IsDeleted, cancellationToken);
            if (row == null)
            {
                row = new UserSetting { Id = Guid.NewGuid(), UserId = userId };
                db.Set<UserSetting>().Add(row);
            }
            row.PriceCountryId = target;
            await db.SaveChangesAsync(cancellationToken);
            return new Result<PriceCountryDto>(HttpStatusCode.OK, await PriceCountries.Load(db, userId, cancellationToken), null);
        }
    }
}
