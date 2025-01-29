namespace Foods.Application.MasterData.Content.ContentLevel.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery(Guid ContentId , Guid LevelId) : IQuery<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id , Guid ContentId, Guid LevelId);
}