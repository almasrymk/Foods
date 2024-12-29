namespace Foods.Application.Abstraction.Query
{
    using MediatR;
    using Foods.Domain.Shared;

    public interface IQuery<TResponse> : IRequest<Result<TResponse>>
    {
    }
}