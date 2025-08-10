namespace Foods.Application.MasterData.Content.ContentTradeMark.Commands.Update
{
    using Foods.Application.Abstraction.Command;
    
    public sealed record UpdateCommand(Guid Id, Guid ContentId, Guid TradeMarkId) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id , Guid ContentId, Guid TradeMarkId);

}