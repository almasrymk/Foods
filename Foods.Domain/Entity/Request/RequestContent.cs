namespace Foods.Domain.Entity.Request
{
    public class RequestContent : BaseEntity
    {
        public Guid RequestItemId { get; set; }
        public Guid ContentId { get; set; }
        public decimal Quantity { get; set; }
    }
}