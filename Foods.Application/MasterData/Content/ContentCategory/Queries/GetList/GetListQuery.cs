namespace Foods.Application.MasterData.Content.ContentCategory.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery() : IQuery<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id, string Name, string? IconPath, string? Color, string? TextColor);
}