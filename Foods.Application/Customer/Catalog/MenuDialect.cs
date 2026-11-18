namespace Foods.Application.Customer.Catalog
{
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData;
    using Foods.Domain.Entity.MasterData.Item;
    using Foods.Domain.Entity.Security;

    /// <param name="CountryId">The customer's own country (names and dialect).</param>
    /// <param name="HomeId">The country the customer is in now (settings, else own): lists start with it.</param>
    /// <param name="Near">Its neighbor countries: they come right after it.</param>
    public sealed record MenuView(Guid? CountryId, string? CountryName, string? Dialect, bool ShowAll, Guid? HomeId = null, IReadOnlyCollection<Guid>? Near = null)
    {
        /// <summary>The country that decides everything but the names and dialect: which dishes show, the recipe version, order and search.</summary>
        public Guid? LiveId => HomeId ?? CountryId;

        /// <summary>0 = the country the customer is in, 1 = a neighbor, 2 = the rest.</summary>
        public int Rank(Guid? countryId)
        {
            if (countryId is not Guid id) return 2;
            if (id == (HomeId ?? CountryId)) return 0;
            return Near != null && Near.Contains(id) ? 1 : 2;
        }
    }

    /// <summary>The customer's own cuisines first: the cuisine's country, or (not set yet) its name matching the country / dialect (مصري، شامي...).</summary>
    public static class CuisineOrder
    {
        public static bool IsMine(Cuisine cuisine, MenuView view) => Rank(cuisine, view) == 0;

        /// <summary>0 = a cuisine of the country the customer is in, 1 = of a neighbor country, 2 = the rest.</summary>
        public static int Rank(Cuisine cuisine, MenuView view)
        {
            if (cuisine.CountryId is Guid country) return view.Rank(country);
            return GuessMine(cuisine, view) ? 0 : 2;
        }

        /// <summary>Cuisine ids by rank (0 and 1), to sort dishes in the database.</summary>
        public static async Task<(List<Guid> Home, List<Guid> Near)> Ranked(IFoodsDbContext db, MenuView view, CancellationToken ct)
        {
            var all = await db.Cuisines.Where(e => !e.IsDeleted).ToListAsync(ct);
            return (all.Where(e => Rank(e, view) == 0).Select(e => e.Id).ToList(), all.Where(e => Rank(e, view) == 1).Select(e => e.Id).ToList());
        }

        private static bool GuessMine(Cuisine cuisine, MenuView view)
        {
            if (view.CountryId is not Guid countryId) return false;
            // Lives in another country: only real cuisine countries count there.
            if (view.HomeId != null && view.HomeId != countryId) return false;
            var name = Norm(cuisine.Name);
            if (name.Length == 0) return false;
            var dialect = Norm(view.Dialect);
            var country = Norm(view.CountryName);
            if (dialect.Length > 0 && name == dialect) return true;
            // مصري ~ مصر، سوري ~ سوريا، مغربي ~ المغرب
            var stem = country.StartsWith("ال") ? country[2..] : country;
            if (stem.EndsWith("ا")) stem = stem[..^1];
            return stem.Length >= 3 && name.Contains(stem);
        }

        public static List<Cuisine> Sort(IEnumerable<Cuisine> cuisines, MenuView view) =>
            cuisines.OrderBy(e => Rank(e, view)).ThenBy(e => e.Sort).ThenBy(e => e.Name).ToList();

        private static string Norm(string? v) => (v ?? "").Trim().Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Replace("ة", "ه").Replace("ى", "ي").Replace("ُ", "");
    }

    public static class MenuDialect
    {
        public static async Task<MenuView> For(IFoodsDbContext db, Guid userId, CancellationToken cancellationToken)
        {
            var user = await db.Set<User>().FirstOrDefaultAsync(e => e.Id == userId && !e.IsDeleted, cancellationToken);
            if (user == null)
                return new MenuView(null, null, null, false);
            if (user.CountryId is not Guid countryId)
                return new MenuView(null, null, null, user.RoleId == RoleIds.Admin);

            var country = await db.Set<Country>().FirstOrDefaultAsync(e => e.Id == countryId && !e.IsDeleted, cancellationToken);
            if (country == null)
                return new MenuView(null, null, null, user.RoleId == RoleIds.Admin);

            var chosen = await db.Set<UserSetting>().Where(e => e.UserId == userId && !e.IsDeleted).Select(e => e.PriceCountryId).FirstOrDefaultAsync(cancellationToken);
            var home = chosen ?? country.Id;
            var neighbors = home == country.Id ? country.Neighbors
                : await db.Set<Country>().Where(e => e.Id == home).Select(e => e.Neighbors).FirstOrDefaultAsync(cancellationToken);
            return new MenuView(country.Id, country.Name, country.Dialect, false, home, ParseIds(neighbors));
        }

        /// <summary>The dishes offered in the country the customer is in (linked to it by the admin).</summary>
        public static async Task<HashSet<Guid>> Offered(IFoodsDbContext db, MenuView view, IEnumerable<Guid> itemIds, CancellationToken cancellationToken)
        {
            if (view.LiveId is not Guid live) return new();
            var ids = itemIds.Distinct().ToList();
            if (ids.Count == 0) return new();
            return (await db.Set<ItemCountry>().Where(e => !e.IsDeleted && e.CountryId == live && ids.Contains(e.ItemId)).Select(e => e.ItemId).ToListAsync(cancellationToken)).ToHashSet();
        }

        public static async Task<Dictionary<Guid, ItemCountry>> ForItems(
            IFoodsDbContext db,
            Guid countryId,
            IEnumerable<Guid> itemIds,
            CancellationToken cancellationToken)
        {
            var ids = itemIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, ItemCountry>();

            var links = await db.Set<ItemCountry>()
                .Where(e => !e.IsDeleted && e.CountryId == countryId && ids.Contains(e.ItemId))
                .ToListAsync(cancellationToken);
            return links.GroupBy(e => e.ItemId).ToDictionary(group => group.Key, group => group.First());
        }

        public static List<Guid> ParseIds(string? value) =>
            (value ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => Guid.TryParse(e, out var id) ? id : Guid.Empty).Where(e => e != Guid.Empty).Distinct().ToList();

        public static string? Pick(string? local, string? fallback) =>
            string.IsNullOrWhiteSpace(local) ? fallback : local.Trim();
    }
}
