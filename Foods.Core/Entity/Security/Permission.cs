namespace Foods.Core.Entity.Security
{
    public class Permission : BaseMasterDataEntity
    {
        public Guid ParentId { get; set; }
        public string? Claim { get; set; }
    }
}