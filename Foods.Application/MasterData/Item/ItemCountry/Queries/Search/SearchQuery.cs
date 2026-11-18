namespace Foods.Application.MasterData.Item.ItemCountry.Queries.Search
{
    using Foods.Application.Abstraction.Query;
    using Foods.Application.MasterData.Item.ItemCountry.Queries.GetById;

    public sealed record SearchQuery(Guid? ItemId, Guid? CountryId) : IQuery<List<GetByIdQueryResponse>>;
}
