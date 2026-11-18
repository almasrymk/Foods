namespace Foods.Domain.Entity.MasterData.Item
{
    public class ItemStep : BaseEntity
    {
        public Guid ItemId { get; set; }
        public int Sort { get; set; }
        public string? Body { get; set; }
        /// <summary>Null = the general recipe; otherwise the row belongs to that country's own version.</summary>
        public Guid? CountryId { get; set; }
    }
}
