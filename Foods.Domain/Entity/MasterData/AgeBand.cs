namespace Foods.Domain.Entity.MasterData
{
    public class AgeBand : BaseMasterDataEntity
    {
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public decimal Factor { get; set; }
    }

    public static class AgeBandIds
    {
        public static readonly Guid Infant = Guid.Parse("a1000001-0000-4000-8000-000000000001");
        public static readonly Guid Child = Guid.Parse("a1000001-0000-4000-8000-000000000002");
        public static readonly Guid Teen = Guid.Parse("a1000001-0000-4000-8000-000000000003");
        public static readonly Guid Adult = Guid.Parse("a1000001-0000-4000-8000-000000000004");
    }
}
