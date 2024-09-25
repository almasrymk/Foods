namespace Foods.Infrastructure.Persistence.Data
{
    using Microsoft.EntityFrameworkCore;

    public class FoodsDbContext : DbContext
    {
        public FoodsDbContext(DbContextOptions<FoodsDbContext> options) : base(options) { }
    }
}