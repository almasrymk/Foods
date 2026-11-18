namespace Foods.Domain.Entity.MasterData.Content
{
    /// <summary>One photo of an ingredient album (shown after the main ImagePath).</summary>
    public class ContentImage : BaseEntity
    {
        public Guid ContentId { get; set; }
        public string? ImagePath { get; set; }
        public int Sort { get; set; }
    }
}
