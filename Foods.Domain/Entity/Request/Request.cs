namespace Foods.Domain.Entity.Request
{
    public class Request : BaseEntity
    {
        public Guid ServiceId { get; set; } 
        public Guid UserId { get; set; }
        public Guid CurrentStepId { get; set; }
        public string? Code { get; set; }
        public DateTime Date { get; set; }      
        public string? Notes { get; set; }
    }
}