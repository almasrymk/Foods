namespace Foods.Application.MasterData.Item.ItemGroup.Queries.Search
{
    using Foods.Application.Abstraction.Query;

    public sealed record SearchQuery(string TextSearch) : IQuery<List<SearchQueryResponse>>;

    public sealed record SearchQueryResponse(Guid Id, string Name, string? IconPath, string? Color, string? TextColor, int Sort = 0);
}