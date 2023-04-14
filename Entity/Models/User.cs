namespace FoodEntity.Models
{
    public class User : BaseModel
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? ImagePath { get; set; }
        public int NumberFamilyMembers { get; set; }
        public long GenderId { get; set; }
        public long JobId { get; set; }
        public long ReligionId { get; set; }
        public bool Active { get; set; }
    }
}