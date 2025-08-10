namespace Foods.Application.MasterData.Content.ContentPrice.Queries.Search
{
    using Foods.Application.Abstraction.Query;

    public sealed record SearchQuery(Guid? ContentId) : IQuery<List<SearchQueryResponse>>;

    public sealed record SearchQueryResponse(Guid Id, Guid ContentId, decimal Price, bool IsCurrent);
}