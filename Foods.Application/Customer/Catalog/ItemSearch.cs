namespace Foods.Application.Customer.Catalog
{
    using System.Text;
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData.Item;

    /// <summary>Advanced search options shared by home, meals and favorites.</summary>
    /// <param name="CategoryIds">More categories picked in the filter (a dish in any of them matches).</param>
    public sealed record ItemSearchOptions(string? Text = null, Guid? CategoryId = null, string? Ingredient = null, Guid? CuisineId = null, Guid? GroupId = null, Guid? MealId = null, IReadOnlyCollection<Guid>? CategoryIds = null)
    {
        public bool IsEmpty => string.IsNullOrWhiteSpace(Text) && CategoryId == null && string.IsNullOrWhiteSpace(Ingredient)
            && CuisineId == null && GroupId == null && MealId == null && (CategoryIds == null || CategoryIds.Count == 0);

        /// <summary>All picked categories (the single one and the list).</summary>
        public HashSet<Guid> Categories
        {
            get
            {
                var set = new HashSet<Guid>(CategoryIds ?? Array.Empty<Guid>());
                if (CategoryId is Guid id) set.Add(id);
                set.Remove(Guid.Empty);
                return set;
            }
        }
    }

    /// <summary>A dish's categories: its main one plus the extra ones the admin added.</summary>
    public static class DishCategories
    {
        public static async Task<Dictionary<Guid, List<Guid>>> Extra(IFoodsDbContext db, IReadOnlyCollection<Guid> itemIds, CancellationToken ct)
        {
            if (itemIds.Count == 0) return new();
            var rows = await db.ItemExtraCategories
                .Where(e => itemIds.Contains(e.ItemId) && !e.IsDeleted)
                .Select(e => new { e.ItemId, e.ItemCategoryId })
                .ToListAsync(ct);
            return rows.GroupBy(e => e.ItemId).ToDictionary(g => g.Key, g => g.Select(e => e.ItemCategoryId).Distinct().ToList());
        }

        /// <summary>Keeps the dishes in any of the given categories (main or extra).</summary>
        public static async Task<List<Item>> In(IFoodsDbContext db, List<Item> items, HashSet<Guid> categories, CancellationToken ct)
        {
            if (categories.Count == 0 || items.Count == 0) return items;
            var extra = await Extra(db, items.Select(e => e.Id).ToList(), ct);
            return items.Where(e => categories.Contains(e.ItemCategoryId)
                || (extra.TryGetValue(e.Id, out var more) && more.Any(categories.Contains))).ToList();
        }

        /// <summary>How many of the dishes are in each category (a dish counts once in each of its categories).</summary>
        public static async Task<Dictionary<Guid, int>> Counts(IFoodsDbContext db, List<Item> items, CancellationToken ct)
        {
            var extra = await Extra(db, items.Select(e => e.Id).ToList(), ct);
            var counts = new Dictionary<Guid, int>();
            foreach (var item in items)
            {
                var all = new HashSet<Guid> { item.ItemCategoryId };
                if (extra.TryGetValue(item.Id, out var more)) all.UnionWith(more);
                foreach (var id in all) counts[id] = counts.GetValueOrDefault(id) + 1;
            }
            return counts;
        }
    }

    /// <summary>In-memory item search with Arabic normalisation (أ/إ/آ=ا، ة=ه، ى=ي، no tashkeel).</summary>
    public static class ItemSearch
    {
        public static string Norm(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var sb = new StringBuilder(value.Length);
            foreach (var raw in value.Trim().ToLowerInvariant())
            {
                var c = raw;
                if (c >= 'ً' && c <= 'ٟ') continue; // tashkeel
                if (c == 'ـ') continue;                  // tatweel
                c = c switch
                {
                    'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
                    'ة' => 'ه',
                    'ى' => 'ي',
                    'ؤ' => 'و',
                    'ئ' => 'ي',
                    _ => c
                };
                sb.Append(char.IsWhiteSpace(c) ? ' ' : c);
            }
            return sb.ToString();
        }

        /// <summary>Letters without long vowels/hamza carriers: "ماحشى" and "محشي" both become "محش".</summary>
        public static string Skeleton(string normalized)
        {
            var sb = new StringBuilder(normalized.Length);
            for (var i = 0; i < normalized.Length; i++)
            {
                var c = normalized[i];
                var wordStart = i == 0 || normalized[i - 1] == ' ' || normalized[i - 1] == '|';
                if (!wordStart && (c == 'ا' || c == 'و' || c == 'ي' || c == 'ء')) continue;
                if (i > 0 && normalized[i - 1] == c && c != ' ') continue; // doubled letters (shadda typed twice)
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Smart word match: exact, then same skeleton, then one typo for words of 4+ letters.</summary>
        public static bool Matches(string hay, string term)
        {
            if (hay.Contains(term)) return true;
            var t = Skeleton(term);
            if (t.Length >= 3 && Skeleton(hay).Contains(t)) return true;
            if (term.Length < 4) return false;
            foreach (var word in hay.Split(new[] { ' ', '|', ',', '،', '-' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (Math.Abs(word.Length - term.Length) > 1) continue;
                if (Distance(word, term) <= 1 || Distance(Skeleton(word), t) <= (t.Length >= 4 ? 1 : 0)) return true;
            }
            return false;
        }

        static int Distance(string a, string b)
        {
            var prev = new int[b.Length + 1];
            var cur = new int[b.Length + 1];
            for (var j = 0; j <= b.Length; j++) prev[j] = j;
            for (var i = 1; i <= a.Length; i++)
            {
                cur[0] = i;
                for (var j = 1; j <= b.Length; j++)
                    cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
                (prev, cur) = (cur, prev);
            }
            return prev[b.Length];
        }

        static string[] Terms(string? value) =>
            Norm(value).Split(new[] { ' ', ',', '،', '+' }, StringSplitOptions.RemoveEmptyEntries);

        public static async Task<List<Item>> Filter(
            IFoodsDbContext db,
            List<Item> items,
            MenuView view,
            ItemSearchOptions options,
            CancellationToken cancellationToken)
        {
            if (items.Count == 0 || options.IsEmpty) return items;

            var picked = options.Categories;
            if (picked.Count > 0)
                items = await DishCategories.In(db, items, picked, cancellationToken);
            if (options.CuisineId is Guid kitchenId)
                items = items.Where(e => e.CuisineId == kitchenId).ToList();
            if (options.GroupId is Guid groupId)
                items = items.Where(e => e.ItemGroupId == groupId).ToList();
            if (options.MealId is Guid mealId && items.Count > 0)
            {
                var inMeal = (await db.ItemMeals.Where(e => e.MealId == mealId && !e.IsDeleted).Select(e => e.ItemId).ToListAsync(cancellationToken)).ToHashSet();
                items = items.Where(e => inMeal.Contains(e.Id)).ToList();
            }

            var words = Terms(options.Text);
            var ingredients = Terms(options.Ingredient);
            if (items.Count == 0 || (words.Length == 0 && ingredients.Length == 0)) return items;

            var ids = items.Select(e => e.Id).ToList();
            var spoken = view.CountryId is Guid countryId
                ? await MenuDialect.ForItems(db, countryId, ids, cancellationToken)
                : new Dictionary<Guid, ItemCountry>();

            var categoryIds = items.Select(e => e.ItemCategoryId).Distinct().ToList();
            var categories = await db.ItemCategories.Where(e => categoryIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => Norm(e.Name), cancellationToken);
            var cuisineIds = items.Where(e => e.CuisineId != null).Select(e => e.CuisineId!.Value).Distinct().ToList();
            var cuisines = await db.Cuisines.Where(e => cuisineIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => Norm(e.Name), cancellationToken);
            var groupIds = items.Select(e => e.ItemGroupId).Distinct().ToList();
            var groups = await db.ItemGroups.Where(e => groupIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => Norm(e.Name), cancellationToken);
            var mealLinks = await (
                from link in db.ItemMeals
                join meal in db.Meals on link.MealId equals meal.Id
                where ids.Contains(link.ItemId) && !link.IsDeleted && !meal.IsDeleted
                select new { link.ItemId, meal.Name }).ToListAsync(cancellationToken);
            var mealNames = mealLinks.GroupBy(e => e.ItemId)
                .ToDictionary(g => g.Key, g => string.Join(" | ", g.Select(e => Norm(e.Name))));

            var links = await (
                from link in db.ItemContents
                join content in db.Contents on link.ContentId equals content.Id
                where ids.Contains(link.ItemId) && !link.IsDeleted && !content.IsDeleted
                select new { link.ItemId, content.Name }).ToListAsync(cancellationToken);
            var contentNames = links
                .GroupBy(e => e.ItemId)
                .ToDictionary(g => g.Key, g => g.Select(e => Norm(e.Name)).Where(e => e.Length > 0).ToList());

            return items.Where(item =>
            {
                contentNames.TryGetValue(item.Id, out var names);
                names ??= new List<string>();

                if (ingredients.Length > 0 && !ingredients.All(term => names.Any(n => Matches(n, term))))
                    return false;
                if (words.Length == 0) return true;

                spoken.TryGetValue(item.Id, out var local);
                var hay = string.Join(" | ", new[]
                {
                    Norm(item.Name), Norm(item.Summary), Norm(item.Description),
                    Norm(local?.Name), Norm(local?.Summary), Norm(local?.Description),
                    categories.GetValueOrDefault(item.ItemCategoryId) ?? "",
                    groups.GetValueOrDefault(item.ItemGroupId) ?? "",
                    mealNames.GetValueOrDefault(item.Id) ?? "",
                    item.CuisineId is Guid c ? cuisines.GetValueOrDefault(c) ?? "" : "",
                    string.Join(" | ", names)
                });
                return words.All(term => Matches(hay, term));
            }).ToList();
        }
    }

    public sealed record GetItemCategoriesQuery() : Foods.Application.Abstraction.Query.IQuery<List<MealItemFilter>>;

    public sealed class GetItemCategoriesQueryHandler(IFoodsDbContext db, Foods.Application.Abstraction.Security.ICurrentUser currentUser) : Foods.Application.Abstraction.Query.IQueryHandler<GetItemCategoriesQuery, List<MealItemFilter>>
    {
        public async Task<Foods.Domain.Shared.Result<List<MealItemFilter>>> Handle(GetItemCategoriesQuery request, CancellationToken cancellationToken)
        {
            // Count only the dishes this customer can see (their country, shown to customers).
            var used = await DishCategories.Counts(db, await CustomerMenu.Visible(db, currentUser, cancellationToken), cancellationToken);
            var list = await db.ItemCategories.Where(e => !e.IsDeleted).OrderBy(e => e.Sort).ThenBy(e => e.Name).ToListAsync(cancellationToken);
            var result = list
                .Where(e => used.ContainsKey(e.Id))
                .Select(e => new MealItemFilter(e.Id, e.Name ?? "", used[e.Id], e.IconPath, e.Color, e.TextColor))
                .ToList();
            return new Foods.Domain.Shared.Result<List<MealItemFilter>>(System.Net.HttpStatusCode.OK, result, null);
        }
    }

    public sealed record GetItemGroupsQuery() : Foods.Application.Abstraction.Query.IQuery<List<MealItemFilter>>;

    public sealed class GetItemGroupsQueryHandler(IFoodsDbContext db, Foods.Application.Abstraction.Security.ICurrentUser currentUser) : Foods.Application.Abstraction.Query.IQueryHandler<GetItemGroupsQuery, List<MealItemFilter>>
    {
        public async Task<Foods.Domain.Shared.Result<List<MealItemFilter>>> Handle(GetItemGroupsQuery request, CancellationToken cancellationToken)
        {
            // Count only the dishes this customer can see (their country, shown to customers).
            var used = (await CustomerMenu.Visible(db, currentUser, cancellationToken))
                .GroupBy(e => e.ItemGroupId)
                .ToDictionary(g => g.Key, g => g.Count());
            var list = await db.ItemGroups.Where(e => !e.IsDeleted).OrderBy(e => e.Sort).ThenBy(e => e.Name).ToListAsync(cancellationToken);
            var result = list
                .Where(e => used.ContainsKey(e.Id))
                .Select(e => new MealItemFilter(e.Id, e.Name ?? "", used[e.Id], e.IconPath, e.Color, e.TextColor))
                .ToList();
            return new Foods.Domain.Shared.Result<List<MealItemFilter>>(System.Net.HttpStatusCode.OK, result, null);
        }
    }
}
