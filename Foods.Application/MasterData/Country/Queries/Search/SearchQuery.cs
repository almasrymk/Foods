namespace Foods.Application.MasterData.Country.Queries.Search
{
    using Foods.Application.Abstraction.Query;

    public sealed record SearchQuery(string TextSearch) : IQuery<List<SearchQueryResponse>>;

    public sealed record SearchQueryResponse(Guid Id, string Name, string? Dialect, string? IconPath, string? Color, string? TextColor, string? Currency = null, string? Neighbors = null);
}
