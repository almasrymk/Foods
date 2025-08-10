namespace Foods.Application.MasterData.Content.Content.Commands.Update
{
    using Foods.Application.Abstraction.Command;
    
    public sealed record UpdateCommand(Guid Id, string Name, Guid ContentCategoryId, Guid ContentGroupId, Guid UnitId, decimal MiniQuantity) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id);

}