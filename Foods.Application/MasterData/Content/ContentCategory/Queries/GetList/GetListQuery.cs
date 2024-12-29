namespace Foods.Application.MasterData.Content.ContentCategory.Queries.GetList
{
    using Foods.Application.Abstraction.QueryList;

    public sealed record GetListQuery(string Name) : IQueryList<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id , string Name);
}