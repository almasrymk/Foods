namespace Foods.Core.Entity.MasterData.Content
{
    public class ContentPrice : BaseEntity
    {
        public Guid ContentId { get; set; }
        public decimal Price { get; set; }
        public bool IsCurrent { get; set; }
    }
}