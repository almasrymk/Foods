namespace Foods.Application.Security.Users.Queries.GetById
{
    using Foods.Application.Abstraction.Query;

    public sealed record GetByIdQuery(Guid Id) : IQuery<GetByIdQueryResponse>;

    public sealed record GetByIdQueryResponse(Guid Id, string Name, string UserName, string Role, Guid? CountryId, string? CountryName, string? Dialect);
}
