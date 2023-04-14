namespace FoodEntity.Models
{
    public class OrderRawMaterial : BaseModel
    {
        public long OrderId { get; set; }
        public long RawMaterialId { get; set; }
        public decimal Quantity { get; set; }
    }
}