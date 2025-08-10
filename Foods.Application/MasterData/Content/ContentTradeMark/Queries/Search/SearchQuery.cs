namespace Foods.Application.MasterData.Content.ContentTradeMark.Queries.Search
{
    using Foods.Application.Abstraction.Query;

    public sealed record SearchQuery(Guid? ContentId, Guid? TradeMarkId) : IQuery<List<SearchQueryResponse>>;

    public sealed record SearchQueryResponse(Guid Id, Guid ContentId, Guid TradeMarkId);
}