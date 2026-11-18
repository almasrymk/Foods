namespace Foods.Application.MasterData.Meal.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery() : IQuery<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id, string Name, string Description, string? ImagePath, string? IconPath, string? Color, string? TextColor, int Sort);
}
