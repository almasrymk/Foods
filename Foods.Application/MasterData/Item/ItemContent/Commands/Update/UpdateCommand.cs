namespace Foods.Application.MasterData.Item.ItemContent.Commands.Update
{
    using Foods.Application.Abstraction.Command;
    
    public sealed record UpdateCommand(Guid Id, Guid ItemId, Guid ContentId, Guid UnitId, decimal Quantity, bool IsOptional = false, bool IsDefault = true) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id);

}