namespace Foods.Core.Entity.MasterData.Content
{
    public class Content : BaseMasterDataEntity
    {
        public Guid ContentCategoryId { get; set; }
        public Guid ContentGroupId { get; set; }
        public Guid UnitId { get; set; }
        public decimal MiniQuantity { get; set; }
    }
}