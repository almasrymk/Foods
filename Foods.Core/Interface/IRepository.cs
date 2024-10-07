using System.Linq.Expressions;

namespace Foods.Core.Interface
{
    public interface IRepository<TEntity> where TEntity : BaseEntity
    {
        /// <summary>
        /// Insert <typeparamref name="TEntity"/> in the database <c>Hi</c>
        /// <![CDATA[HH ee ff]]> 
        /// <code>
        /// sss
        /// dsss
        /// rrr
        /// </code>
        /// <inheritdoc>dddeeeee</inheritdoc>
        /// <list type="bullet">ssss ssssddd</list>
        /// <list type="number">
        /// ssss 
        /// ssssddd
        /// </list>
        /// <list type="number">ssxss sxsssddd</list>
        /// <list type="table">ssss ssssddd</list>
        /// <see cref="int"/>
        /// <>description</example>
        /// </summary>
        /// <param name="Ob" ><typeparamref name="TEntity"/></param>
        /// <returns><typeparamref name="TEntity"/> after entered</returns>
        ValueTask<TEntity> CreateAsync(TEntity Ob);
        ValueTask<bool> UpdateAsync(TEntity Ob);
        ValueTask<TEntity?> GetByFilterAsync(Expression<Func<TEntity, bool>> Filter);
        ValueTask<IEnumerable<TEntity>?> GetListByFilterAsync(Expression<Func<TEntity, bool>> Filter);
        ValueTask<bool> DeleteAsync(Expression<Func<TEntity, bool>> Filter);
        ValueTask<bool> ShiftDeleteAsync(Expression<Func<TEntity, bool>> Filter);
        ValueTask<bool> Commit();
    }
}