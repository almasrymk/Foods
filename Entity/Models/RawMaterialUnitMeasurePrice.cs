namespace FoodEntity.Models
{
    public class RawMaterialUnitMeasurePrice : BaseDataModel
    {
        public long RawMaterialUnitMeasureId { get; set; }
        public DateTime Date { get; set; }       
        public decimal Price { get; set; }
        public bool CurrentPrice { get; set; }
    }
}