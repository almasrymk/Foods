namespace Foods.Application.MasterData.Level.Queries.Search
{
    using Foods.Application.Abstraction.Query;

    public sealed record SearchQuery(string txtSearch) : IQuery<List<SearchQueryResponse>>;

    public sealed record SearchQueryResponse(Guid Id, string Name);
}