namespace Foods.Application.Abstraction.Command
{
    using MediatR;
    using Foods.Domain.Shared;

    public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result> where TCommand : ICommand
    {
    }

    public interface ICommandHander<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>> where TCommand : ICommand<TResponse>
    {
    }
}