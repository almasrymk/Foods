namespace Foods.Application.MasterData.Content.ContentAlternative.Queries.Search
{
    using Foods.Application.Abstraction.Query;

    public sealed record SearchQuery(Guid? ContentId, Guid? ContentAlternativeId) : IQuery<List<SearchQueryResponse>>;

    public sealed record SearchQueryResponse(Guid Id, Guid ContentId, Guid ContentAlternativeId);
}