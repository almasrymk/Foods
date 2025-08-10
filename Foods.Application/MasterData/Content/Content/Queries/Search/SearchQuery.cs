namespace Foods.Application.MasterData.Content.Content.Queries.Search
{
    using Foods.Application.Abstraction.Query;

    public sealed record SearchQuery(string TextSearch , Guid? ContentCategoryId, Guid? ContentGroupId) : IQuery<List<SearchQueryResponse>>;

    public sealed record SearchQueryResponse(Guid Id, string Name, Guid ContentCategoryId, Guid ContentGroupId, Guid UnitId, decimal MiniQuantity);
}