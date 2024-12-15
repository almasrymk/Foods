namespace Foods.Domain.Entity.MasterData.Item
{
    public class ItemContent : BaseEntity
    {
        public Guid ItemId { get; set; }
        public Guid ContentId { get; set; }
        public Guid UnitId { get; set; }
        public decimal Quantity { get; set; }
    }
}