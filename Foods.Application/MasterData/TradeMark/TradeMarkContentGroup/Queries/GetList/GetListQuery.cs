namespace Foods.Application.MasterData.TradeMark.TradeMarkContentGroup.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery(Guid? ContentGroupId, Guid? TradeMarkId) : IQuery<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id , string Name, Guid ContentGroupId, Guid TradeMarkId);
}