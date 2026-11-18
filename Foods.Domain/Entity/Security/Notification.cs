namespace Foods.Domain.Entity.Security
{
    /// <summary>An in-app notification for one user. Key dedupes generated notifications.</summary>
    public class Notification : BaseEntity
    {
        public Guid UserId { get; set; }
        public string Type { get; set; } = "";
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? Link { get; set; }
        /// <summary>Optional picture (uploaded path or client asset), e.g. the family member on a birthday.</summary>
        public string? Image { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? Key { get; set; }
    }
}
