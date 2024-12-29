namespace Foods.Application.Abstraction.Query
{
    using MediatR;
    using Foods.Domain.Shared;

    public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>> where TQuery : IQuery<TResponse>
    {

    }    
}