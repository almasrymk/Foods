namespace Foods.Domain.Entity.Request
{
    public class RequestShoppingLine : BaseEntity
    {
        public Guid RequestId { get; set; }
        public Guid ContentId { get; set; }
        public Guid? PurchasePlaceId { get; set; }
        public Guid UnitId { get; set; }
        public decimal Quantity { get; set; }
    }
}
