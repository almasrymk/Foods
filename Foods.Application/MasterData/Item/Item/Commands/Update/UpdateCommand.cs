namespace Foods.Application.MasterData.Item.Item.Commands.Update
{
    using Foods.Application.Abstraction.Command;
    public sealed record UpdateCommand(Guid Id, string Name, Guid ItemCategoryId, Guid ItemGroupId, string? Summary, string? Description) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id);   
}