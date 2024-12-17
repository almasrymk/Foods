namespace Foods.Infrastructure.Persistence.UnitOfWork
{
    using Foods.Domain;
    using Foods.Domain.Abstraction;

    public class UnitOfWork<TEntity>(IFoodsDbContext dbContext) : IUnitOfWork<TEntity> where TEntity : BaseEntity
    {
        public IRepository<TEntity> _Repository => new Repository<TEntity>(dbContext);       

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