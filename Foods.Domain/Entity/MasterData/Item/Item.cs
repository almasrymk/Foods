namespace Foods.Domain.Entity.MasterData.Item
{
    public class Item : BaseMasterDataEntity
    {
        public Guid ItemCategoryId { get; set; }
        public Guid? CuisineId { get; set; }
        public Guid ItemGroupId { get; set; }
        public string? Summary { get; set; }
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        /// <summary>Shown in the customer's search and meal lists. False = only used as a part (صنف) of other dishes.</summary>
        public bool ShowToCustomer { get; set; } = true;
        /// <summary>Order set by the admin (smaller first).</summary>
        public int Sort { get; set; }
    }
}