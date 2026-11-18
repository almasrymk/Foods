namespace Foods.Application.MasterData.Cuisine.Queries.GetById
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetByIdQuery(Guid Id) : IQuery<GetByIdQueryResponse>;

    public sealed record GetByIdQueryResponse(Guid Id, string Name, string? IconPath, string? Color, string? TextColor, int Sort, string? ImagePath = null, Guid? CountryId = null);
}
