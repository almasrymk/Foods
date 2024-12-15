namespace Foods.Application.MasterData.Content.ContentCategory.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery(string Name) : IQueryList<GetListQueryResponse>;

    public sealed record GetListQueryResponse(Guid Id , string Name);
}