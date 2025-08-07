namespace Foods.Application.MasterData.Content.ContentAlternative.Commands.Delete
{
    using Foods.Application.Abstraction.Command;

    public sealed  record DeleteCommand(Guid Id) : ICommand;
}