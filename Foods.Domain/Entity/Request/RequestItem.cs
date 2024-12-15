namespace Foods.Domain.Entity.Request
{
    public class RequestItem : BaseEntity
    {
        public Guid ItemId { get; set; }
        public Guid RequestId { get; set; }
        public decimal Quantity { get; set; }
    }
}