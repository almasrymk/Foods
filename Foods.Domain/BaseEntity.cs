namespace Foods.Domain
{
    public class BaseEntity
    {        
        public virtual Guid Id { get; set; }
        public bool IsDeleted { get; set; }
        public int Status { get; set; }
    }
}