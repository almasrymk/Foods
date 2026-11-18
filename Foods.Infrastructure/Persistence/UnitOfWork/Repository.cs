namespace Foods.Infrastructure.Persistence.UnitOfWork
{
    using Foods.Domain;  
    using System.Linq.Expressions;
    using Foods.Domain.Abstraction;
    using Microsoft.EntityFrameworkCore;
    using System;

    public class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
    {
        private readonly IFoodsDbContext dbContext;
        private readonly DbSet<TEntity> dbEntity;

        public Repository(IFoodsDbContext _dbContext)
        {
            dbContext = _dbContext;
            dbEntity = dbContext.Set<TEntity>();
        }

        public async ValueTask<bool> Commit()
        {
            if (await dbContext.SaveChangesAsync() > 0)
                return true;
            return false;
        }

        public virtual async ValueTask<TEntity> CreateAsync(TEntity Ob)
        {
            await dbEntity.AddAsync(Ob);
            return Ob;
        }

        public virtual async ValueTask<bool> DeleteAsync(Expression<Func<TEntity, bool>> Filter)
        {
            var Ob = await dbEntity.FirstOrDefaultAsync(Filter);
            if (Ob != null)
            {
                Ob.IsDeleted = true;
                dbEntity.Attach(Ob);
                return true;
            }

            return false;
        }

        public virtual async ValueTask<TEntity?> GetByFilterAsync(Expression<Func<TEntity, bool>> Filter)
        {
            return await dbEntity.Where(e => !e.IsDeleted).FirstOrDefaultAsync(Filter);
        }

        public virtual async ValueTask<IEnumerable<TEntity>?> GetListByFilterAsync()
        {
            return await dbEntity.Where(e => !e.IsDeleted).ToListAsync();
        }

        public virtual async ValueTask<IEnumerable<TEntity>?> GetListByFilterAsync(Expression<Func<TEntity, bool>> Filter)
        {
            return await dbEntity.Where(e => !e.IsDeleted).Where(Filter).ToListAsync();
        }

        public virtual async ValueTask<bool> ShiftDeleteAsync(Expression<Func<TEntity, bool>> Filter)
        {
            var Ob = await dbEntity.FirstOrDefaultAsync(Filter);
            if (Ob != null)
            {
                dbEntity.Remove(Ob);
                return true;
            }

            return false;
        }

        public virtual async ValueTask<bool> UpdateAsync(TEntity Ob)
        {
            var tracked = dbEntity.Local.FirstOrDefault(e => e.Id == Ob.Id);
            if (tracked != null)
            {
                dbEntity.Entry(tracked).State = EntityState.Modified;
                return true;
            }

            if (!await dbEntity.AnyAsync(e => e.Id == Ob.Id))
                return false;

            dbEntity.Update(Ob);
            return true;
        }
    }
}