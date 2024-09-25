namespace Foods.Infrastructure.Persistence
{
    using Foods.Core.Interface;
    using Microsoft.EntityFrameworkCore;
    using Foods.Infrastructure.Persistence.Data;
    using System.Linq.Expressions;
    using System.Collections.Immutable;
    using System.Runtime.CompilerServices;

    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private FoodsDbContext dbContext;
        private DbSet<TEntity> dbEntity;

        public Repository(FoodsDbContext _dbContext)
        {
            dbContext = _dbContext;
            dbEntity = dbContext.Set<TEntity>();
        }

        public virtual async ValueTask<TEntity> CreateAsync(TEntity Ob)
        {
            await dbEntity.AddAsync(Ob);
            return Ob;
        }

        public virtual ValueTask<bool> DeleteAsync(Func<TEntity, bool> Filter)
        {
            throw new NotImplementedException();
        }

        public virtual async ValueTask<TEntity?> GetByFilterAsync(Expression<Func<TEntity, bool>> Filter)
        {
            return await dbEntity.FirstOrDefaultAsync(Filter);
        }

        public virtual async ValueTask<IEnumerable<TEntity>?> GetListByFilterAsync(Func<TEntity, bool> Filter)
        {
            return await dbEntity.Where(Filter).AsQueryable().ToListAsync();
        }

        public virtual async ValueTask<bool> ShiftDeleteAsync(Func<TEntity, bool> Filter)
        {
            throw new NotImplementedException();
        }

        public virtual async ValueTask<bool> UpdateAsync(TEntity Ob)
        {
            if(await dbEntity.AnyAsync(e=>e == Ob))
            {
                dbEntity.Attach(Ob);
                return true;
            }
                
            return false;
        }
    }
}