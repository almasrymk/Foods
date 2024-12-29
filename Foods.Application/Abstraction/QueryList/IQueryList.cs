namespace Foods.Application.Abstraction.QueryList
{
    using MediatR;
    using Foods.Domain.Shared;

    public interface IQueryList<TResponse> : IRequest<List<Result<TResponse>>>
    {
    }
}