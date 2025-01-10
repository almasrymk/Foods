namespace Foods.Application.Abstraction.Query
{
    using MediatR;
    using Foods.Domain.Shared;

    public interface IQueryListHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>> where TQuery : IQuery<TResponse>
    {

    }
}