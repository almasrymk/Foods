namespace Foods.Application.Security.Users.Queries.GetById
{
    using System.Net;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using Foods.Domain.Shared;

    public sealed class GetByIdQueryHandler(IRepository<User> repository, IRepository<Foods.Domain.Entity.MasterData.Country> countries) : IQueryHandler<GetByIdQuery, GetByIdQueryResponse>
    {
        public async Task<Result<GetByIdQueryResponse>> Handle(GetByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await repository.GetByFilterAsync(e => e.Id == request.Id);
            if (user == null)
                return new Result<GetByIdQueryResponse>(HttpStatusCode.BadRequest, null, new List<string> { "المستخدم غير موجود" });

            var country = user.CountryId == null ? null : await countries.GetByFilterAsync(e => e.Id == user.CountryId);
            return new Result<GetByIdQueryResponse>(
                HttpStatusCode.OK,
                new GetByIdQueryResponse(user.Id, user.Name ?? "", user.UserName ?? "", UserRoles.Name(user.RoleId), user.CountryId, country?.Name, country?.Dialect),
                null);
        }
    }
}
