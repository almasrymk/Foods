namespace Foods.Application.MasterData.TradeMark.TradeMarkContentGroup.Commands.Delete
{
    using Foods.Application.Abstraction.Command;

    public sealed  record DeleteCommand(Guid Id) : ICommand;
}