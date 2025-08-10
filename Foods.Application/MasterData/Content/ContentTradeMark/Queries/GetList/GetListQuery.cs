namespace Foods.Application.MasterData.Content.ContentTradeMark.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery(Guid? ContentId, Guid? TradeMarkId ): IQuery<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id ,  Guid ContentId, Guid TradeMarkId);
}