namespace Foods.Application.Customer.Ordering
{
    /// <param name="CountryId">The country version of the dish the customer picked (from a country's cuisine); null = their own country.</param>
    public sealed record ItemSelection(Guid ItemId, decimal RepeatCount, int? PeopleCount, Guid? CountryId = null)
    {
        public static async Task<Foods.Application.Customer.Catalog.RecipeCountries> Scope(Foods.Domain.Abstraction.IFoodsDbContext db, Guid userId, IEnumerable<ItemSelection> items, CancellationToken ct)
        {
            var scope = await Foods.Application.Customer.Catalog.CountryVersions.Default(db, userId, ct);
            foreach (var item in items)
                if (item.CountryId is Guid country && country != Guid.Empty) scope.PerItem[item.ItemId] = country;
            return scope;
        }
    }

    public sealed record ShoppingLineResult(
        Guid ContentId,
        string? Name,
        string? ImagePath,
        Guid? PurchasePlaceId,
        string? PurchasePlaceName,
        Guid UnitId,
        string? UnitName,
        decimal Quantity,
        int PurchaseMode = 0,
        string? PackageName = null,
        decimal PurchaseStep = 0,
        decimal PurchaseIncrement = 0,
        List<string>? Images = null,
        decimal PurchaseUnitPrice = 0,
        decimal PurchaseMinAmount = 0,
        decimal? Price = null,
        decimal? UnitPrice = null);

    /// <summary>
    /// "أقل وحدة شراء": rounds a needed quantity up to what can really be bought.
    /// 1 package: whole packages (1.4 L of oil -> 2 bottles of 1 L).
    /// 2 kilo fractions: only 1/8, 1/4, 1/3, 1/2, 3/4 (and whole kilos + those), at or above the item minimum.
    /// 3 money: the least money amount (e.g. 20 EGP of basil); the grams are not used.
    /// 4 money under 1/8 kg: below 1/8 kg the real grams are kept (the client shows a money amount),
    ///   from 1/8 kg on it rounds up to the allowed kilo fractions.
    /// </summary>
    public static class PurchaseRules
    {
        public const int Normal = 0, Package = 1, KiloFraction = 2, Money = 3, MoneyThenKilo = 4;

        public static decimal Step(int mode, decimal step) => mode switch
        {
            Package => step > 0 ? step : 1,
            KiloFraction => step > 0 ? step : 125,
            Money => step > 0 ? step : 10,
            MoneyThenKilo => step > 0 ? step : 125,
            _ => 0
        };

        /// <param name="unitPrice">Mode 4: price of one step (1/8 kg). 0 = keep the real grams below 1/8 kg.</param>
        /// <param name="minAmount">Mode 4: least money amount (0 = the increment).</param>
        /// <param name="increment">Mode 4: money step (0 = 5 EGP).</param>
        public static decimal Round(int mode, decimal step, decimal needed, decimal unitPrice = 0, decimal minAmount = 0, decimal increment = 0)
        {
            if (mode == MoneyThenKilo)
            {
                var kiloUnit = step > 0 ? step : 125;
                if (needed <= 0) return 0;
                if (needed >= kiloUnit) return KiloUp(kiloUnit, needed);
                if (unitPrice <= 0) return needed;
                var inc = increment > 0 ? increment : 5;
                var min = minAmount > 0 ? minAmount : inc;
                var money = needed / kiloUnit * unitPrice;
                // Small tolerance so grams saved with 3 decimals do not jump to the next money step.
                money = Math.Max(min, Math.Ceiling(money / inc - 0.001m) * inc);
                if (money >= unitPrice) return kiloUnit;
                return Math.Round(money / unitPrice * kiloUnit, 3);
            }
            if (mode == Normal || needed <= 0) return needed;
            var unit = Step(mode, step);
            if (mode == Money) return unit;
            if (mode == KiloFraction) return KiloUp(unit, needed);
            return Math.Ceiling(needed / unit) * unit;
        }

        /// <summary>The kilo fractions people really buy (grams): 1/8, 1/4, 1/3, 1/2, 3/4.</summary>
        private static readonly decimal[] Fractions = { 125m, 250m, 333.333m, 500m, 750m };
        private const decimal Epsilon = 0.01m;

        /// <summary>Fractions at or above the item minimum (min 1/4 drops 1/8; min 1 kilo keeps whole kilos only).</summary>
        public static IReadOnlyList<decimal> AllowedFractions(decimal min)
            => Fractions.Where(f => f >= min - Epsilon).ToList();

        /// <summary>Smallest allowed value (k kilos + an allowed fraction, never 0) at or above the needed grams.</summary>
        public static decimal KiloUp(decimal min, decimal needed)
        {
            if (needed <= 0) return 0;
            var fractions = AllowedFractions(min);
            var k = Math.Floor(needed / 1000m);
            var candidates = new List<decimal>();
            if (k > 0) candidates.Add(k * 1000m);
            candidates.AddRange(fractions.Select(f => k * 1000m + f));
            candidates.Add((k + 1) * 1000m);
            return candidates.Where(v => v >= needed - Epsilon).Min();
        }
    }

    public sealed record ShoppingLineInput(Guid ContentId, Guid UnitId, decimal Quantity);
}
