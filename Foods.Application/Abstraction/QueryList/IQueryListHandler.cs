namespace Foods.Application.Abstraction.QueryList
{
    using MediatR;
    using Foods.Domain.Shared;

    public interface IQueryListHandler<TQuery, TResponse> : IRequestHandler<TQuery, List<Result<TResponse>>> where TQuery : IQueryList<TResponse>
    {

    }
}