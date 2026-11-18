namespace Foods.Domain.Entity.Security
{
    /// <summary>Per-user app preferences (one row per user).</summary>
    public class UserSetting : BaseEntity
    {
        public Guid UserId { get; set; }
        public string Language { get; set; } = "ar";
        public bool NotifyLogin { get; set; } = true;
        public bool NotifyWelcome { get; set; } = true;
        public bool NotifyBirthday { get; set; } = true;
        public bool NotifyOrders { get; set; } = true;
        public bool NotifyPlanning { get; set; } = true;
        /// <summary>The customer works with prices (dish / order costs from ingredient prices).</summary>
        public bool UsePrices { get; set; }
        /// <summary>The country the customer lives in now (prices and currency). Null = their own country.</summary>
        public Guid? PriceCountryId { get; set; }
    }
}
