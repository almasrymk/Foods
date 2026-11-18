namespace Foods.Domain.Entity.MasterData.Item
{
    /// <summary>
    /// A customer's choice for an optional ingredient (Kind 0, RefId = ContentId) or an optional part
    /// (Kind 1, RefId = PartItemId) of a dish. Only stored when it differs from the dish default.
    /// </summary>
    public class UserItemChoice : BaseEntity
    {
        public const int ContentKind = 0;
        public const int PartKind = 1;

        public Guid UserId { get; set; }
        public Guid ItemId { get; set; }
        public int Kind { get; set; }
        public Guid RefId { get; set; }
        public bool Selected { get; set; }
        /// <summary>The country version this edit belongs to.</summary>
        public Guid? CountryId { get; set; }
    }
}
