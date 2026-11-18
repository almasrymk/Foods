namespace Foods.Domain.Entity.MasterData.Item
{
    public class ItemCountry : BaseEntity
    {
        public Guid ItemId { get; set; }
        public Guid CountryId { get; set; }
        public bool IsPrimary { get; set; }
        public string? Name { get; set; }
        public string? Summary { get; set; }
        public string? Description { get; set; }
        /// <summary>True = this country has its own ingredients / parts / steps / videos (rows with CountryId = this country).</summary>
        public bool OwnRecipe { get; set; }
    }
}
