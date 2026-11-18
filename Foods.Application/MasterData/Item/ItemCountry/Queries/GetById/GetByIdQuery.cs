namespace Foods.Application.MasterData.Item.ItemCountry.Queries.GetById
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetByIdQuery(Guid Id) : IQuery<GetByIdQueryResponse>;

    public sealed record GetByIdQueryResponse(Guid Id, Guid ItemId, Guid CountryId, bool IsPrimary, string? Name, string? Summary, string? Description);
}
