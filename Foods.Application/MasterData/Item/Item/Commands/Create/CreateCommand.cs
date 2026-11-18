namespace Foods.Application.MasterData.Item.Item.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(string Name , Guid ItemCategoryId , Guid? CuisineId , Guid ItemGroupId , string? Summary , string? Description, string? ImagePath, bool ShowToCustomer = true, int Sort = 0, List<Guid>? CategoryIds = null) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);

}