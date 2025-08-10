namespace Foods.Application.MasterData.Content.ContentAlternative.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery(Guid? ContentId, Guid? ContentAlternativeId) : IQuery<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id , Guid ContentId, Guid ContentAlternativeId);
}