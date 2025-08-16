namespace Foods.Application.MasterData.Item.ItemLevel.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(Guid ItemId , Guid LevelId) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);

}