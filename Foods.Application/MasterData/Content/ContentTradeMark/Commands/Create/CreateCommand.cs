namespace Foods.Application.MasterData.Content.ContentTradeMark.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(Guid ContentId , Guid TradeMarkId) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);

}