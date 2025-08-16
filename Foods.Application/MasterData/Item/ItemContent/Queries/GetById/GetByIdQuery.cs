namespace Foods.Application.MasterData.Item.ItemContent.Queries.GetById
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetByIdQuery(Guid Id) : IQuery<GetByIdQueryResponse>;

    public sealed record GetByIdQueryResponse(Guid Id, Guid ItemId, Guid ContentId, Guid UnitId, decimal Quantity);
}
