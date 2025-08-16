namespace Foods.Application.MasterData.Item.ItemLevel.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery() : IQuery<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id , Guid ItemId, Guid LevelId);
}