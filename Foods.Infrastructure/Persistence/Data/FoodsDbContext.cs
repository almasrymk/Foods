namespace Foods.Infrastructure.Persistence.Data
{
    using Foods.Core.Entity.MasterData;
    using Microsoft.EntityFrameworkCore;
    using Foods.Core.Entity.MasterData.Item;
    using Foods.Core.Entity.MasterData.Content;
    using Foods.Core.Entity.MasterData.TradeMark;

    public class FoodsDbContext(DbContextOptions<FoodsDbContext> options) : DbContext(options)
    {
        public virtual DbSet<Content> Contents { get; set; }
        public virtual DbSet<ContentAlternative> ContentAlternatives { get; set; }
        public virtual DbSet<ContentCategory> ContentCategories { get; set; }
        public virtual DbSet<ContentGroup> ContentGroup { get; set; }
        public virtual DbSet<ContentLevel> ContentLevels { get; set; }
        public virtual DbSet<ContentPrice> ContentPrices { get; set; }
        public virtual DbSet<ContentTradeMark> ContentTradeMarks { get; set; }
        public virtual DbSet<Item> Items { get; set; }
        public virtual DbSet<ItemCategory> ItemCategories { get; set; }
        public virtual DbSet<ItemContent> ItemContents { get; set; }
        public virtual DbSet<ItemGroup> ItemGroups { get; set; }
        public virtual DbSet<ItemLevel> ItemLevels { get; set; }
        public virtual DbSet<ItemMeal> ItemMeals { get; set; }
        public virtual DbSet<TradeMark> TradeMarks { get; set; }
        public virtual DbSet<TradeMarkContentGroup> TradeMarkContentGroups { get; set; }
        public virtual DbSet<Level> Levels { get; set; }
        public virtual DbSet<Meal> Meals { get; set; }
        public virtual DbSet<Unit> Units { get; set; }
    }
}