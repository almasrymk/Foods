namespace Foods.Domain.Entity.MasterData.Content
{
    public class Content : BaseMasterDataEntity
    {
        public Guid ContentCategoryId { get; set; } 
        /// <summary>Order set by the admin (smaller first).</summary>
        public int Sort { get; set; }
        public Guid ContentGroupId { get; set; }
        public Guid UnitId { get; set; }
        public decimal MiniQuantity { get; set; }
        public string? ImagePath { get; set; }
        public Guid? PurchasePlaceId { get; set; }
        /// <summary>How it is bought: 0 normal (grams/count), 1 package (bottle...), 2 kilo fractions (1/8 kg...), 3 by money (EGP).</summary>
        public int PurchaseMode { get; set; }
        /// <summary>Package name for mode 1, e.g. زجاجة / علبة / كيس.</summary>
        public string? PackageName { get; set; }
        /// <summary>Mode 1: package size in the recipe unit (1000 ml). Mode 2: smallest step in grams (125 = 1/8 kg). Mode 3: least money (20 EGP).</summary>
        public decimal PurchaseStep { get; set; }
        /// <summary>Mode 3: how much each + adds (EGP). 0 = same as PurchaseStep.</summary>
        public decimal PurchaseIncrement { get; set; }
        /// <summary>Mode 4: price of one PurchaseStep (1/8 kg) in EGP. 0 = old fixed-amount behaviour.</summary>
        public decimal PurchaseUnitPrice { get; set; }
        /// <summary>Mode 4: the least money amount (EGP). 0 = the increment.</summary>
        public decimal PurchaseMinAmount { get; set; }
        /// <summary>Admin price (EGP) of PriceQuantity of PriceUnitId (null = the content unit), e.g. 120 for 1000 جرام. 0 = no price.</summary>
        public decimal Price { get; set; }
        public decimal PriceQuantity { get; set; } = 1;
        public Guid? PriceUnitId { get; set; }
        /// <summary>The cuisine this ingredient belongs to (like a dish).</summary>
        public Guid? CuisineId { get; set; }
    }
}