namespace Foods.Application.Abstraction.Command
{
    using MediatR;
    using Foods.Domain.Shared;

    public interface ICommand : IRequest<Result>
    {

    }
     
    public interface ICommand<TResponse> : IRequest<Result<TResponse>>
    {

    }
}