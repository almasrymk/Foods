namespace Foods.Domain.Entity.MasterData
{
    public class Meal : BaseMasterDataEntity
    {
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public string? IconPath { get; set; }
        public string? Color { get; set; }
        public string? TextColor { get; set; }
        public int Sort { get; set; }
    }
}
