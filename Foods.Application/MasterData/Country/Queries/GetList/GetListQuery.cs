namespace Foods.Application.MasterData.Country.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery() : IQuery<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id, string Name, string? Dialect, string? IconPath, string? Color, string? TextColor, string? Currency = null, string? Neighbors = null);
}
