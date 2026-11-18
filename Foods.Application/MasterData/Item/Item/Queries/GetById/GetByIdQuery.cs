namespace Foods.Application.MasterData.Item.Item.Queries.GetById
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetByIdQuery(Guid Id) : IQuery<GetByIdQueryResponse>;

    public sealed record GetByIdQueryResponse(Guid Id, string Name, Guid ItemCategoryId, Guid? CuisineId, Guid ItemGroupId, string? Summary, string? Description, string? ImagePath, bool ShowToCustomer = true, int Sort = 0, List<Guid>? CategoryIds = null);
}
