using System.Linq.Expressions;

namespace Foods.Core.Interface
{
    public interface IRepository<TEntity> where TEntity : class
    {
        ValueTask<TEntity> CreateAsync(TEntity Ob);
        ValueTask<TEntity> UpdateAsync(TEntity Ob);
        ValueTask<TEntity?> GetByFilterAsync(Expression<Func<TEntity, bool>> Filter);
        ValueTask<IEnumerable<TEntity>?> GetListByFilterAsync(Func<TEntity, bool> Filter);
        ValueTask<bool> DeleteAsync(Func<TEntity, bool> Filter);
        ValueTask<bool> ShiftDeleteAsync(Func<TEntity, bool> Filter);
    }
}