namespace Entity
{
    public class OrderItem : BaseModel
    {
        public long OrderId { get; set; }
        public long ItemId { get; set; }
        public decimal Quantity { get; set; }
    }
}