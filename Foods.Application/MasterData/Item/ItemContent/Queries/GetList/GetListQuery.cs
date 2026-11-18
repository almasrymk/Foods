namespace Foods.Application.MasterData.Item.ItemContent.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery(Guid? ItemId, Guid? ContentId, Guid? UnitId) : IQuery<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id, Guid ItemId, Guid ContentId, Guid UnitId, decimal Quantity, bool IsOptional = false, bool IsDefault = true);
}