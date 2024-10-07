namespace Foods.Core.Entity.Request
{
    public class RequestTrack : BaseEntity
    {
        public Guid RequestId { get; set; }
        public Guid ServiceStepId { get; set; }
        public Guid NextServiceStepId { get; set; }
    }
}