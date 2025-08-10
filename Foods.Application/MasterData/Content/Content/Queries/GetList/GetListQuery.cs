namespace Foods.Application.MasterData.Content.Content.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery(Guid? ContentCategoryId, Guid? ContentGroupId) : IQuery<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id, string Name, Guid ContentCategoryId, Guid ContentGroupId, Guid UnitId, decimal MiniQuantity);
}