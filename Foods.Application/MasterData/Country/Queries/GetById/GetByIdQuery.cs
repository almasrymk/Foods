namespace Foods.Application.MasterData.Country.Queries.GetById
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetByIdQuery(Guid Id) : IQuery<GetByIdQueryResponse>;

    public sealed record GetByIdQueryResponse(Guid Id, string Name, string? Dialect, string? IconPath, string? Color, string? TextColor, string? Currency = null, string? Neighbors = null);
}
