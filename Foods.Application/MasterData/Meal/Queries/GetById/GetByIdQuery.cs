namespace Foods.Application.MasterData.Meal.Queries.GetById
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetByIdQuery(Guid Id) : IQuery<GetByIdQueryResponse>;

    public sealed record GetByIdQueryResponse(Guid Id, string Name , string Description);
}
