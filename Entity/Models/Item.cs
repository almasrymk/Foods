namespace FoodEntity.Models
{
    public class Item : BaseDataModel
    {
        public long ItemCategoryId { get; set; }

        public string? Desciption { get; set; }

        public string? ItemContent { get; set; }
    }
}