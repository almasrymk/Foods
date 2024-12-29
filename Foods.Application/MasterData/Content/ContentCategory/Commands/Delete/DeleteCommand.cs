namespace Foods.Application.MasterData.Content.ContentCategory.Commands.Delete
{
    using Foods.Application.Abstraction.Command;

    public sealed  record DeleteCommand(Guid Id) : ICommand<bool>;
}