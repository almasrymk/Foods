namespace Foods.Application.MasterData.Item.Item.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery() : IQuery<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id , string Name, string? ImagePath, Guid ItemCategoryId, Guid? CuisineId, Guid ItemGroupId, string? Summary, bool ShowToCustomer = true, int Sort = 0, List<Guid>? CategoryIds = null);
}