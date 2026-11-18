namespace Foods.Application.MasterData.Meal.Queries.Search
{
    using Foods.Application.Abstraction.Query;

    public sealed record SearchQuery(string TextSearch) : IQuery<List<SearchQueryResponse>>;

    public sealed record SearchQueryResponse(Guid Id, string Name, string Description, string? ImagePath, string? IconPath, string? Color, string? TextColor, int Sort);
}
