namespace Foods.Application.MasterData.TradeMark.TradeMark.Commands.Delete
{
    using Foods.Application.Abstraction.Command;

    public sealed  record DeleteCommand(Guid Id) : ICommand;
}