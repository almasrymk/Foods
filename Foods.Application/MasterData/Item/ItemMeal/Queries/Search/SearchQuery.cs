namespace Foods.Application.MasterData.Item.ItemMeal.Queries.Search
{
    using Foods.Application.Abstraction.Query;

    public sealed record SearchQuery(Guid? ItemId, Guid? MealId) : IQuery<List<SearchQueryResponse>>;

    public sealed record SearchQueryResponse(Guid Id, Guid ItemId, Guid MealId);
}