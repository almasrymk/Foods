namespace Foods.Domain.Entity.MasterData.Item
{
    public class ItemVideo : BaseEntity
    {
        public Guid ItemId { get; set; }
        public int Sort { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
        /// <summary>Null = the general recipe; otherwise the row belongs to that country's own version.</summary>
        public Guid? CountryId { get; set; }
    }
}
