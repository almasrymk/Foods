namespace Foods.Application.MasterData.Item.ItemLevel.Queries.Search
{
    using Foods.Application.Abstraction.Query;

    public sealed record SearchQuery(Guid? ItemId, Guid? LevelId) : IQuery<List<SearchQueryResponse>>;

    public sealed record SearchQueryResponse(Guid Id, Guid ItemId, Guid LevelId);
}