namespace Foods.Application.MasterData.Item.ItemCategory.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery(string Name) : IQuery<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id , string Name);
}