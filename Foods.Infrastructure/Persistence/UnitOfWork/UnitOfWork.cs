namespace Foods.Infrastructure.Persistence.UnitOfWork
{
    using Foods.Domain.Abstraction;

    public class UnitOfWork(IFoodsDbContext dbContext): IUnitOfWork
    {
        public async Task<int> SaveChangeAsync(CancellationToken cancellationToken = default)
        {            
            return await dbContext.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            //this.Dispose();
        }
    }
}