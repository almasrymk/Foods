namespace Foods.Domain.Entity.MasterData.Content
{
    /// <summary>An ingredient in one country: its name and description in that country's dialect and its price in that country's currency.</summary>
    public class ContentCountry : BaseEntity
    {
        public Guid ContentId { get; set; }
        public Guid CountryId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        /// <summary>Price of PriceQuantity of PriceUnitId (null = the content unit) in the country's currency. 0 = no price.</summary>
        public decimal Price { get; set; }
        public decimal PriceQuantity { get; set; } = 1;
        public Guid? PriceUnitId { get; set; }
    }
}
