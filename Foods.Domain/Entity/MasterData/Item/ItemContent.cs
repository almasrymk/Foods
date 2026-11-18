namespace Foods.Domain.Entity.MasterData.Item
{
    public class ItemContent : BaseEntity
    {
        public Guid ItemId { get; set; }
        public Guid ContentId { get; set; }
        public Guid UnitId { get; set; }
        public decimal Quantity { get; set; }
        /// <summary>Optional ingredient: the customer can add or remove it.</summary>
        public bool IsOptional { get; set; }
        /// <summary>For an optional ingredient: selected by default.</summary>
        public bool IsDefault { get; set; } = true;
        /// <summary>Null = the general recipe; otherwise the row belongs to that country's own version.</summary>
        public Guid? CountryId { get; set; }
    }
}