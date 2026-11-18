namespace Foods.Domain.Entity.MasterData.Content
{
    /// <summary>A customer's own price for an ingredient (أسعار المكونات in settings). Overrides the admin price for that customer only.</summary>
    public class UserContentPrice : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid ContentId { get; set; }
        /// <summary>Money (EGP) for <see cref="PriceQuantity"/> of <see cref="PriceUnitId"/>, e.g. 120 for 1 كيلو.</summary>
        public decimal Price { get; set; }
        public decimal PriceQuantity { get; set; } = 1;
        public Guid? PriceUnitId { get; set; }
        /// <summary>The country the price is for (the customer can live in another country later).</summary>
        public Guid? CountryId { get; set; }
    }
}
