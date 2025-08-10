namespace Foods.Application.MasterData.Content.ContentPrice.Commands.Update
{
    using Foods.Application.Abstraction.Command;
    
    public sealed record UpdateCommand(Guid Id, Guid ContentId, decimal Price, bool IsCurrent) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id);

}