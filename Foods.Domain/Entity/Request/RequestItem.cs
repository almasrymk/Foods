namespace Foods.Domain.Entity.Request
{
    public class RequestItem : BaseEntity
    {
        public Guid ItemId { get; set; }
        public Guid RequestId { get; set; }
        public decimal Quantity { get; set; }
        public decimal RepeatCount { get; set; }
        public int? PeopleCount { get; set; }
        /// <summary>The country version of the recipe that was ordered (null = the customer's own country).</summary>
        public Guid? CountryId { get; set; }
    }
}