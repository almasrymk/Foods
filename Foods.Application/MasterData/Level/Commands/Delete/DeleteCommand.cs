namespace Foods.Application.MasterData.Level.Commands.Delete
{
    using Foods.Application.Abstraction.Command;

    public sealed  record DeleteCommand(Guid Id) : ICommand;
}