namespace Foods.Application.MasterData.Item.ItemCountry.Queries.GetList
{
    using Foods.Application.Abstraction.Query;
    using Foods.Application.MasterData.Item.ItemCountry.Queries.GetById;

    public sealed record GetListQuery(Guid? ItemId, Guid? CountryId) : IQuery<List<GetByIdQueryResponse>>;
}
