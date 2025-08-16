namespace Foods.Application.MasterData.Item.ItemContent.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(Guid ItemId , Guid ContentId , Guid UnitId , decimal Quantity) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);

}