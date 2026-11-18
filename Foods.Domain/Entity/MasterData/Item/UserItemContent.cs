namespace Foods.Domain.Entity.MasterData.Item
{
    /// <summary>
    /// A customer's personal change to a dish ingredient line (visible to that customer only).
    /// Adds an ingredient, changes a base line's quantity/unit, or removes a base line (IsRemoved).
    /// </summary>
    public class UserItemContent : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid ItemId { get; set; }
        public Guid ContentId { get; set; }
        public Guid UnitId { get; set; }
        public decimal Quantity { get; set; }
        public bool IsRemoved { get; set; }
        /// <summary>The country version this edit belongs to.</summary>
        public Guid? CountryId { get; set; }
    }
}
