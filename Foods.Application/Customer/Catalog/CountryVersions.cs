namespace Foods.Application.Customer.Catalog
{
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using Foods.Domain.Entity.MasterData;
    using Foods.Domain.Entity.MasterData.Item;
    using Foods.Domain.Entity.MasterData.Content;

    /// <summary>
    /// Which country version of each dish is wanted: the cuisine's country when the customer opened the dish
    /// from a country's cuisine, otherwise the customer's own country (Default).
    /// </summary>
    public sealed class RecipeCountries(Guid? @default)
    {
        public Guid? Default { get; } = @default;
        public Dictionary<Guid, Guid?> PerItem { get; } = new();
        public Guid? For(Guid itemId) => PerItem.TryGetValue(itemId, out var country) ? country : Default;

        public static RecipeCountries Of(Guid? country, Guid itemId)
        {
            var result = new RecipeCountries(country);
            result.PerItem[itemId] = country;
            return result;
        }
    }

    /// <summary>
    /// Per-country versions of dishes and ingredients.
    /// A dish row with CountryId = null is the general recipe; a country with ItemCountry.OwnRecipe uses its own rows.
    /// Names always follow the customer's own country (dialect); prices follow the country the customer lives in.
    /// </summary>
    public static class CountryVersions
    {
        public const string DefaultCurrency = "ج.م";

        public static async Task<Guid?> OwnCountry(IFoodsDbContext db, Guid userId, CancellationToken ct)
            => userId == Guid.Empty ? null : await db.Set<User>().Where(e => e.Id == userId).Select(e => e.CountryId).FirstOrDefaultAsync(ct);

        /// <summary>The country the customer is in now (settings), else their own: it decides everything but names and dialect.</summary>
        public static async Task<Guid?> HomeCountry(IFoodsDbContext db, Guid userId, CancellationToken ct)
        {
            if (userId == Guid.Empty) return null;
            var chosen = await db.Set<UserSetting>().Where(e => e.UserId == userId && !e.IsDeleted).Select(e => e.PriceCountryId).FirstOrDefaultAsync(ct);
            return chosen ?? await OwnCountry(db, userId, ct);
        }

        public static async Task<RecipeCountries> Default(IFoodsDbContext db, Guid userId, CancellationToken ct)
            => new(await HomeCountry(db, userId, ct));

        /// <summary>The cuisine's country when one is picked (and has a country), otherwise the country the customer is in.</summary>
        public static async Task<Guid?> ForCuisine(IFoodsDbContext db, Guid userId, Guid? cuisineId, CancellationToken ct)
        {
            if (cuisineId is Guid id)
            {
                var country = await db.Cuisines.Where(e => e.Id == id && !e.IsDeleted).Select(e => e.CountryId).FirstOrDefaultAsync(ct);
                if (country != null) return country;
            }
            return await HomeCountry(db, userId, ct);
        }

        /// <summary>A country sent by the client (the version shown in the details); unknown or empty = the country the customer is in.</summary>
        public static async Task<Guid?> Check(IFoodsDbContext db, Guid userId, Guid? countryId, CancellationToken ct)
        {
            if (countryId is Guid id && id != Guid.Empty && await db.Countries.AnyAsync(e => e.Id == id && !e.IsDeleted, ct))
                return id;
            return await HomeCountry(db, userId, ct);
        }

        /// <summary>For each dish: the country whose rows are used (the wanted one when it has its own recipe, otherwise null = general).</summary>
        public static async Task<Dictionary<Guid, Guid?>> RowCountries(IFoodsDbContext db, RecipeCountries wanted, IReadOnlyCollection<Guid> itemIds, CancellationToken ct)
        {
            var ids = itemIds.Distinct().ToList();
            var result = ids.ToDictionary(e => e, _ => (Guid?)null);
            var countries = ids.Select(wanted.For).Where(e => e != null).Select(e => e!.Value).Distinct().ToList();
            if (countries.Count == 0) return result;
            var own = await db.ItemCountries
                .Where(e => ids.Contains(e.ItemId) && countries.Contains(e.CountryId) && e.OwnRecipe && !e.IsDeleted)
                .Select(e => new { e.ItemId, e.CountryId })
                .ToListAsync(ct);
            foreach (var id in ids)
            {
                var country = wanted.For(id);
                if (country != null && own.Any(e => e.ItemId == id && e.CountryId == country)) result[id] = country;
            }
            return result;
        }

        public static async Task<List<ItemContent>> Contents(IFoodsDbContext db, Dictionary<Guid, Guid?> rows, CancellationToken ct)
        {
            var ids = rows.Keys.ToList();
            var countries = rows.Values.Where(e => e != null).Select(e => e!.Value).Distinct().ToList();
            var all = await db.ItemContents
                .Where(e => ids.Contains(e.ItemId) && !e.IsDeleted && (e.CountryId == null || countries.Contains(e.CountryId.Value)))
                .ToListAsync(ct);
            return all.Where(e => e.CountryId == rows[e.ItemId]).ToList();
        }

        public static async Task<List<ItemPart>> Parts(IFoodsDbContext db, Dictionary<Guid, Guid?> rows, CancellationToken ct)
        {
            var ids = rows.Keys.ToList();
            var countries = rows.Values.Where(e => e != null).Select(e => e!.Value).Distinct().ToList();
            var all = await db.ItemParts
                .Where(e => ids.Contains(e.ItemId) && !e.IsDeleted && (e.CountryId == null || countries.Contains(e.CountryId.Value)))
                .OrderBy(e => e.Sort)
                .ToListAsync(ct);
            return all.Where(e => e.CountryId == rows[e.ItemId]).ToList();
        }

        public static Task<List<ItemStep>> Steps(IFoodsDbContext db, Guid itemId, Guid? rowCountry, CancellationToken ct)
            => db.ItemSteps.Where(e => e.ItemId == itemId && !e.IsDeleted && e.CountryId == rowCountry).OrderBy(e => e.Sort).ToListAsync(ct);

        public static Task<List<ItemVideo>> Videos(IFoodsDbContext db, Guid itemId, Guid? rowCountry, CancellationToken ct)
            => db.ItemVideos.Where(e => e.ItemId == itemId && !e.IsDeleted && e.CountryId == rowCountry).OrderBy(e => e.Sort).ToListAsync(ct);

        /// <summary>Ingredient rows of one country (names / descriptions in its dialect, its price).</summary>
        public static async Task<Dictionary<Guid, ContentCountry>> ContentsIn(IFoodsDbContext db, Guid? countryId, IEnumerable<Guid> contentIds, CancellationToken ct)
        {
            if (countryId is not Guid country) return new();
            var ids = contentIds.Distinct().ToList();
            if (ids.Count == 0) return new();
            var rows = await db.ContentCountries.Where(e => e.CountryId == country && ids.Contains(e.ContentId) && !e.IsDeleted).ToListAsync(ct);
            return rows.GroupBy(e => e.ContentId).ToDictionary(g => g.Key, g => g.First());
        }

        /// <summary>An ingredient's name for the customer: the dialect name of their own country, else the general name.</summary>
        public static string? Name(Dictionary<Guid, ContentCountry> spoken, Guid contentId, string? fallback)
            => spoken.TryGetValue(contentId, out var row) ? MenuDialect.Pick(row.Name, fallback) : fallback;

        /// <summary>The country the customer lives in now (settings), else their own country, with its currency.</summary>
        public static async Task<(Guid? CountryId, string Currency)> PriceCountry(IFoodsDbContext db, Guid userId, CancellationToken ct)
        {
            var chosen = userId == Guid.Empty ? null
                : await db.Set<UserSetting>().Where(e => e.UserId == userId && !e.IsDeleted).Select(e => e.PriceCountryId).FirstOrDefaultAsync(ct);
            var countryId = chosen ?? await OwnCountry(db, userId, ct);
            var currency = countryId is Guid id
                ? await db.Countries.Where(e => e.Id == id).Select(e => e.Currency).FirstOrDefaultAsync(ct)
                : null;
            return (countryId, string.IsNullOrWhiteSpace(currency) ? DefaultCurrency : currency.Trim());
        }

        /// <summary>The general (admin) price is in the default currency, so it is only used where that currency is used.</summary>
        public static bool UsesGeneralPrice(string currency) => currency == DefaultCurrency;

        public static async Task<string?> CountryName(IFoodsDbContext db, Guid? countryId, CancellationToken ct)
            => countryId is Guid id ? await db.Countries.Where(e => e.Id == id).Select(e => e.Name).FirstOrDefaultAsync(ct) : null;
    }
}
