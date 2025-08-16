namespace Foods.Application.MasterData.Item.ItemContent.Queries.Search
{
    using Foods.Application.Abstraction.Query;

    public sealed record SearchQuery(Guid? ItemId, Guid? ContentId, Guid? UnitId) : IQuery<List<SearchQueryResponse>>;

    public sealed record SearchQueryResponse(Guid Id, Guid ItemId, Guid ContentId, Guid UnitId, decimal Quantity);
}