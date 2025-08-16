namespace Foods.Application.MasterData.TradeMark.TradeMarkContentGroup.Commands.Update
{
    using Foods.Application.Abstraction.Command;
    
    public sealed record UpdateCommand(Guid Id, string Name, Guid ContentGroupId, Guid TradeMarkId) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id);

}