namespace Foods.Domain.Entity.MasterData.Item
{
    /// <summary>One photo of a dish album (shown after the main ImagePath).</summary>
    public class ItemImage : BaseEntity
    {
        public Guid ItemId { get; set; }
        public string? ImagePath { get; set; }
        public int Sort { get; set; }
    }
}
