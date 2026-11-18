namespace Foods.Domain.Entity.MasterData.Item
{
    public class ItemCategory : BaseMasterDataEntity
    {
        public string? IconPath { get; set; }
        public string? Color { get; set; }
        public string? TextColor { get; set; }

        /// <summary>Order set by the admin (smaller first).</summary>
        public int Sort { get; set; }
    }
}
