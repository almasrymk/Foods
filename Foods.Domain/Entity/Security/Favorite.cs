namespace Foods.Domain.Entity.Security
{
    public class Favorite : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid ItemId { get; set; }
    }
}
