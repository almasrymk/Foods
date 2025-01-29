namespace Foods.Application.MasterData.Content.ContentLevel.Queries.Search
{
    using Foods.Application.Abstraction.Query;

    public sealed record SearchQuery(Guid ContentId, Guid LevelId) : IQuery<List<SearchQueryResponse>>;

    public sealed record SearchQueryResponse(Guid Id, Guid ContentId, Guid LevelId);
}