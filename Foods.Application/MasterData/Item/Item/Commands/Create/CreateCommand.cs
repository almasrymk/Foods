namespace Foods.Application.MasterData.Item.Item.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(string Name , Guid ItemCategoryId , Guid ItemGroupId , string? Summary , string? Description) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);

}