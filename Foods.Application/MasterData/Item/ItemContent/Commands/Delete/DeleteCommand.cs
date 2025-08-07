namespace Foods.Application.MasterData.Item.ItemContent.Commands.Delete
{
    using Foods.Application.Abstraction.Command;

    public sealed  record DeleteCommand(Guid Id) : ICommand;
}