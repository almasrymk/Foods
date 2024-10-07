namespace Foods.Infrastructure.Persistence.Data
{
    using Microsoft.EntityFrameworkCore;

    public class FoodsDbContext(DbContextOptions<FoodsDbContext> options) : DbContext(options)
    {
    }
}