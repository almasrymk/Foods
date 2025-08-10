namespace Foods.Application.MasterData.Content.Content.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(string Name , Guid ContentCategoryId , Guid ContentGroupId , Guid UnitId , decimal MiniQuantity) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);

}