namespace Foods.Domain.Entity.MasterData
{
    public class Country : BaseMasterDataEntity
    {
        public string? Dialect { get; set; }
        public string? IconPath { get; set; }
        public string? Color { get; set; }
        public string? TextColor { get; set; }
        /// <summary>Currency shown with prices for customers in this country, e.g. ج.م / ر.س.</summary>
        public string? Currency { get; set; }
        /// <summary>Neighbor country ids, comma separated (ordering: the country, then its neighbors, then the rest).</summary>
        public string? Neighbors { get; set; }
    }

    public static class CountryIds
    {
        public static readonly Guid Jordan = Guid.Parse("a1111111-1111-4111-8111-111111111105");
        public static readonly Guid Emirates = Guid.Parse("a1111111-1111-4111-8111-111111111107");
        public static readonly Guid Bahrain = Guid.Parse("a1111111-1111-4111-8111-111111111111");
        public static readonly Guid Tunisia = Guid.Parse("a1111111-1111-4111-8111-11111111110a");
        public static readonly Guid Algeria = Guid.Parse("a1111111-1111-4111-8111-11111111110b");
        public static readonly Guid Comoros = Guid.Parse("a1111111-1111-4111-8111-111111111117");
        public static readonly Guid Djibouti = Guid.Parse("a1111111-1111-4111-8111-111111111115");
        public static readonly Guid Saudi = Guid.Parse("a1111111-1111-4111-8111-111111111106");
        public static readonly Guid Sudan = Guid.Parse("a1111111-1111-4111-8111-111111111113");
        public static readonly Guid Syria = Guid.Parse("a1111111-1111-4111-8111-111111111102");
        public static readonly Guid Somalia = Guid.Parse("a1111111-1111-4111-8111-111111111114");
        public static readonly Guid Iraq = Guid.Parse("a1111111-1111-4111-8111-11111111110c");
        public static readonly Guid Oman = Guid.Parse("a1111111-1111-4111-8111-11111111110e");
        public static readonly Guid Palestine = Guid.Parse("a1111111-1111-4111-8111-111111111104");
        public static readonly Guid Qatar = Guid.Parse("a1111111-1111-4111-8111-11111111110f");
        public static readonly Guid Kuwait = Guid.Parse("a1111111-1111-4111-8111-111111111108");
        public static readonly Guid Lebanon = Guid.Parse("a1111111-1111-4111-8111-111111111103");
        public static readonly Guid Libya = Guid.Parse("a1111111-1111-4111-8111-111111111112");
        public static readonly Guid Egypt = Guid.Parse("a1111111-1111-4111-8111-111111111101");
        public static readonly Guid Morocco = Guid.Parse("a1111111-1111-4111-8111-111111111109");
        public static readonly Guid Mauritania = Guid.Parse("a1111111-1111-4111-8111-111111111116");
        public static readonly Guid Yemen = Guid.Parse("a1111111-1111-4111-8111-11111111110d");

        public static readonly (Guid Id, string Name, string Dialect)[] All =
        {
            (Jordan, "الأردن", "شامي"),
            (Emirates, "الإمارات", "خليجي"),
            (Bahrain, "البحرين", "خليجي"),
            (Tunisia, "تونس", "تونسي"),
            (Algeria, "الجزائر", "جزائري"),
            (Comoros, "جزر القمر", "قمري"),
            (Djibouti, "جيبوتي", "جيبوتي"),
            (Saudi, "السعودية", "خليجي"),
            (Sudan, "السودان", "سوداني"),
            (Syria, "سوريا", "شامي"),
            (Somalia, "الصومال", "صومالي"),
            (Iraq, "العراق", "عراقي"),
            (Oman, "عُمان", "خليجي"),
            (Palestine, "فلسطين", "شامي"),
            (Qatar, "قطر", "خليجي"),
            (Kuwait, "الكويت", "خليجي"),
            (Lebanon, "لبنان", "شامي"),
            (Libya, "ليبيا", "ليبي"),
            (Egypt, "مصر", "مصري"),
            (Morocco, "المغرب", "مغربي"),
            (Mauritania, "موريتانيا", "حساني"),
            (Yemen, "اليمن", "يمني")
        };
    }
}
