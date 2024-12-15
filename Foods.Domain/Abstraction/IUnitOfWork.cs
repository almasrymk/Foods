namespace Foods.Domain.Abstraction
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangeAsync(CancellationToken cancellationToken = default);
    }
}