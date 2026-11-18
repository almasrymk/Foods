namespace Foods.Application.MasterData.Item.ItemContent.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(Guid ItemId , Guid ContentId , Guid UnitId , decimal Quantity, bool IsOptional = false, bool IsDefault = true) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);

}