namespace FoodEntity.Models
{
    public class BaseDataModel : BaseModel
    {
        public virtual long Code { get; set; }
        public string? Name { get; set; }  
        public string? IconPath { get; set; }
    }
}