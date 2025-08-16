namespace Foods.Application.MasterData.TradeMark.TradeMarkContentGroup.Queries.Search
{
    using Foods.Application.Abstraction.Query;

    public sealed record SearchQuery(string TextSearch, Guid ContentGroupId, Guid TradeMarkId) : IQuery<List<SearchQueryResponse>>;

    public sealed record SearchQueryResponse(Guid Id, string Name, Guid ContentGroupId, Guid TradeMarkId);
}