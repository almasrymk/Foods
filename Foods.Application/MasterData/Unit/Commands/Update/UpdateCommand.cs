namespace Foods.Application.MasterData.Unit.Commands.Update
{
    using Foods.Application.Abstraction.Command;
    
    public sealed record UpdateCommand(Guid Id, string Name, string? NickName, decimal BasicQuantity, bool BeDivided) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id);

}