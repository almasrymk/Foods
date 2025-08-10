namespace Foods.Application.MasterData.Content.ContentPrice.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery(Guid? ContentId) : IQuery<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id , Guid ContentId, decimal Price, bool IsCurrent);
}