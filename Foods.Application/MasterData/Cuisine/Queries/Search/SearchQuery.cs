namespace Foods.Application.MasterData.Cuisine.Queries.Search
{
    using Foods.Application.Abstraction.Query;

    public sealed record SearchQuery(string TextSearch) : IQuery<List<SearchQueryResponse>>;

    public sealed record SearchQueryResponse(Guid Id, string Name, string? IconPath, string? Color, string? TextColor, int Sort, string? ImagePath = null, Guid? CountryId = null);
}
