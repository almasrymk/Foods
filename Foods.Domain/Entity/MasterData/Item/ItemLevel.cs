namespace Foods.Domain.Entity.MasterData.Item
{
    public class ItemLevel : BaseEntity
    {
        public Guid ItemId { get; set; }
        public Guid LevelId { get; set; }
    }
}