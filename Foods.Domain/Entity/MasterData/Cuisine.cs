namespace Foods.Domain.Entity.MasterData
{
    public class Cuisine : BaseMasterDataEntity
    {
        public string? IconPath { get; set; }
        public string? ImagePath { get; set; }
        public string? Color { get; set; }
        public string? TextColor { get; set; }
        public int Sort { get; set; }
        /// <summary>The country this cuisine belongs to (مصري → مصر): its customers see it first.</summary>
        public Guid? CountryId { get; set; }
    }
}
