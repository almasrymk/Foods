namespace Foods.Application.MasterData.Unit.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery() : IQuery<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id, string Name, string? NickName, decimal BasicQuantity, bool BeDivided);
}