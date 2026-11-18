namespace Foods.Domain.Entity.MasterData.Item
{
    /// <summary>Another category of a dish (its main one is Item.ItemCategoryId).</summary>
    public class ItemExtraCategory : BaseEntity
    {
        public Guid ItemId { get; set; }
        public Guid ItemCategoryId { get; set; }
    }
}
