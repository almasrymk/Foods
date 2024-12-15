namespace Foods.Application.Abstraction.Query
{
    using MediatR;
    using Foods.Domain.Shared;

    public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>> where TQuery : IQuery<TResponse>
    {

    }

    public interface IQueryListHandler<TQuery, TResponse> : IRequestHandler<TQuery, List<Result<TResponse>>> where TQuery : IQueryList<TResponse>
    {

    }
}