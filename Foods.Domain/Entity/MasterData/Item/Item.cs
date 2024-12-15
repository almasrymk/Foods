namespace Foods.Domain.Entity.MasterData.Item
{
    public class Item : BaseMasterDataEntity
    {
        public Guid ItemCategoryId { get; set; }
        public Guid ItemGroupId { get; set; }
        public string? Summary { get; set; }
        public string? Description { get; set; }
    }
}