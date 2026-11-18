namespace Foods.Domain.Entity.MasterData.Item
{
    /// <summary>A customer's personal recipe step; when any exist they replace the dish steps for that customer.</summary>
    public class UserItemStep : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid ItemId { get; set; }
        public int Sort { get; set; }
        public string? Body { get; set; }
        /// <summary>The country version this edit belongs to.</summary>
        public Guid? CountryId { get; set; }
    }
}
