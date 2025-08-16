namespace Foods.Application.MasterData.Unit.Queries.Search
{
    using Foods.Application.Abstraction.Query;

    public sealed record SearchQuery(string TextSearch) : IQuery<List<SearchQueryResponse>>;

    public sealed record SearchQueryResponse(Guid Id, string Name, string? NickName, decimal BasicQuantity, bool BeDivided);
}