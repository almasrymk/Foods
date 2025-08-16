namespace Foods.Application.MasterData.TradeMark.TradeMarkContentGroup.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(string Name , Guid ContentGroupId , Guid TradeMarkId) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);

}