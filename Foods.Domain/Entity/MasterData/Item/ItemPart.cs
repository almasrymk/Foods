namespace Foods.Domain.Entity.MasterData.Item
{
    /// <summary>
    /// "صنف": another dish that is part of this dish (e.g. rice with a main dish).
    /// Required parts are always included; optional ones can be added or removed by the customer.
    /// </summary>
    public class ItemPart : BaseEntity
    {
        public Guid ItemId { get; set; }
        public Guid PartItemId { get; set; }
        public bool IsOptional { get; set; }
        /// <summary>For an optional part: selected by default.</summary>
        public bool IsDefault { get; set; } = true;
        public int Sort { get; set; }
        /// <summary>Null = the general recipe; otherwise the row belongs to that country's own version.</summary>
        public Guid? CountryId { get; set; }
    }
}
