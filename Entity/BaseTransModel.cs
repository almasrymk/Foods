namespace FoodEntity.Models
{
    public class BaseTransModel : BaseModel
    {
        public virtual long Code { get; set; }
        public virtual DateTime Date { get; set; }
        public long UserCreateId { get; set; }
        public long UserUpdateId { get; set; }
        public DateTime UserCreateDate { get; set; }
        public DateTime UserUpdateDate { get; set; }
    }
}