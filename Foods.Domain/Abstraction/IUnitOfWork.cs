namespace Foods.Domain.Abstraction
{
    public interface IUnitOfWork<TEntity> : IDisposable where TEntity : BaseEntity
    {
        IRepository<TEntity> _Repository { get; }
        Task<int> SaveChangeAsync(CancellationToken cancellationToken = default);
    }
}