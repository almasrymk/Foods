namespace Foods.Application.MasterData.Item.ItemCategory.Commands.Delete
{
    using Foods.Application.Abstraction.Command;

    public sealed  record DeleteCommand(Guid Id) : ICommand;
}