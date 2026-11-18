namespace Foods.Domain.Entity.Security
{
    public class User : BaseMasterDataEntity
    {
        public Guid RoleId { get; set; }
        public Guid? CountryId { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? Token { get; set; }        
        public string? ImagePath { get; set; }
        /// <summary>Last successful customer login (null = not tracked yet).</summary>
        public DateTime? LastLoginAt { get; set; }
    }
}