namespace Foods.Infrastructure.Persistence.Data
{
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Request;
    using Foods.Domain.Entity.Security;
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Entity.MasterData;
    using Foods.Domain.Entity.MasterData.Item;
    using Foods.Domain.Entity.MasterData.Content;
    using Foods.Domain.Entity.MasterData.TradeMark;

    public class FoodsDbContext(DbContextOptions<FoodsDbContext> options) : DbContext(options), IFoodsDbContext
    {
        #region MasterData
        #region Content
        public virtual DbSet<Content> Contents { get; set; }
        public virtual DbSet<ContentAlternative> ContentAlternatives { get; set; }
        public virtual DbSet<ContentCategory> ContentCategories { get; set; }
        public virtual DbSet<ContentGroup> ContentGroup { get; set; }
        public virtual DbSet<ContentLevel> ContentLevels { get; set; }
        public virtual DbSet<ContentPrice> ContentPrices { get; set; }
        public virtual DbSet<ContentTradeMark> ContentTradeMarks { get; set; }
        #endregion

        #region Item
        public virtual DbSet<Item> Items { get; set; }
        public virtual DbSet<ItemCategory> ItemCategories { get; set; }
        public virtual DbSet<ItemContent> ItemContents { get; set; }
        public virtual DbSet<ItemGroup> ItemGroups { get; set; }
        public virtual DbSet<ItemLevel> ItemLevels { get; set; }
        public virtual DbSet<ItemMeal> ItemMeals { get; set; }
        #endregion

        #region TradeMark
        public virtual DbSet<TradeMark> TradeMarks { get; set; }
        public virtual DbSet<TradeMarkContentGroup> TradeMarkContentGroups { get; set; }
        #endregion
        public virtual DbSet<Level> Levels { get; set; }
        public virtual DbSet<Meal> Meals { get; set; }
        public virtual DbSet<Unit> Units { get; set; }
        #endregion

        #region Request
        public virtual DbSet<Foods.Domain.Entity.Request.Request> Requests { get; set; }
        public virtual DbSet<Foods.Domain.Entity.Request.RequestContent> RequestContents { get; set; }
        public virtual DbSet<RequestItem> RequestItems { get; set; }
        public virtual DbSet<RequestTrack> RequestTracks { get; set; }
        #endregion

        #region Security
        public virtual DbSet<Permission> Permissions { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<RolePermission> RolePermissions { get; set; }
        public virtual DbSet<User> Users { get; set; }
        #endregion
         
        public DbSet<TEntity> Set<TEntity>() where TEntity : class
        {
            return base.Set<TEntity>();
        }

        public override int SaveChanges()
        {
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }
        public override void Dispose()
        {
            //this.Dispose();
        }
    }
}