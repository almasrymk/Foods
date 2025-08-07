namespace Foods.Application.MasterData.Item.ItemMeal.Queries.GetList
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetListQuery(string Name) : IQuery<List<GetListQueryResponse>>;

    public sealed record GetListQueryResponse(Guid Id , string Name);
}