namespace Foods.Core.Entity.Security
{
    public class Users : BaseMasterDataEntity
    {
        public Guid RoleId { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? Token { get; set; }        
        public string? ImagePath { get; set; }
    }
}