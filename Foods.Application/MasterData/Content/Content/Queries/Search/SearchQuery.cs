namespace Foods.Application.MasterData.Content.Content.Queries.Search
{
    using Foods.Application.Abstraction.Query;

    public sealed record SearchQuery(string TextSearch , Guid? ContentCategoryId, Guid? ContentGroupId) : IQuery<List<SearchQueryResponse>>;

    public sealed record SearchQueryResponse(Guid Id, string Name, Guid ContentCategoryId, Guid ContentGroupId, Guid UnitId, decimal MiniQuantity, string? ImagePath, Guid? PurchasePlaceId, int PurchaseMode = 0, string? PackageName = null, decimal PurchaseStep = 0, decimal PurchaseIncrement = 0, decimal PurchaseUnitPrice = 0, decimal PurchaseMinAmount = 0, decimal Price = 0, decimal PriceQuantity = 1, Guid? PriceUnitId = null, Guid? CuisineId = null, int Sort = 0);
}