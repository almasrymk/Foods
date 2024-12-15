namespace Foods.Domain.Entity.MasterData.Item
{
    public class ItemMeal : BaseEntity
    {
        public Guid ItemId { get; set; }
        public Guid MealId { get; set; }
    }
}