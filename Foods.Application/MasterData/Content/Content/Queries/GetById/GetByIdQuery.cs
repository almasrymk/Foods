namespace Foods.Application.MasterData.Content.Content.Queries.GetById
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetByIdQuery(Guid Id) : IQuery<GetByIdQueryResponse>;

    public sealed record GetByIdQueryResponse(Guid Id, string Name , Guid ContentCategoryId, Guid ContentGroupId, Guid UnitId, decimal MiniQuantity);
}
