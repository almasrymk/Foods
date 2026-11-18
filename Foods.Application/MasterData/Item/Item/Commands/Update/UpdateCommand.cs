namespace Foods.Application.MasterData.Item.Item.Commands.Update
{
    using Foods.Application.Abstraction.Command;
    public sealed record UpdateCommand(Guid Id, string Name, Guid ItemCategoryId, Guid? CuisineId, Guid ItemGroupId, string? Summary, string? Description, string? ImagePath, bool ShowToCustomer = true, int Sort = 0, List<Guid>? CategoryIds = null) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id);   
}