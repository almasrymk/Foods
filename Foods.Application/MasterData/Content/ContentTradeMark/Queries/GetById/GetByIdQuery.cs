namespace Foods.Application.MasterData.Content.ContentTradeMark.Queries.GetById
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetByIdQuery(Guid Id) : IQuery<GetByIdQueryResponse>;

    public sealed record GetByIdQueryResponse(Guid Id, Guid ContentId, Guid TradeMarkId);
}
