namespace Foods.Application.MasterData.Item.ItemCategory.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery() : IQuery<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id, string Name, string? IconPath, string? Color, string? TextColor, int Sort = 0);
}