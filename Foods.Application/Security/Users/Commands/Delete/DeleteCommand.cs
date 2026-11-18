namespace Foods.Application.Security.Users.Commands.Delete
{
    using Foods.Application.Abstraction.Command;

    public sealed record DeleteCommand(Guid Id) : ICommand;
}
