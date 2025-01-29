namespace Foods.Application.MasterData.Content.ContentLevel.Commands.Delete
{
    using Foods.Application.Abstraction.Command;

    public sealed  record DeleteCommand(Guid Id) : ICommand;
}