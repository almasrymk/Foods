namespace Foods.Application.Security.Users.Queries.GetList
{
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using Foods.Domain.Shared;

    public sealed class GetListQueryHandler(IRepository<User> repository, IRepository<Foods.Domain.Entity.MasterData.Country> countries) : IQueryHandler<GetListQuery, List<GetListQueryResponse>>
    {
        public async Task<Result<List<GetListQueryResponse>>> Handle(GetListQuery request, CancellationToken cancellationToken)
        {
            var list = await repository.GetListByFilterAsync() ?? new List<User>();
            var countryList = await countries.GetListByFilterAsync() ?? new List<Foods.Domain.Entity.MasterData.Country>();
            var names = countryList.ToDictionary(e => e.Id);
            var rows = list
                .OrderBy(user => user.Name)
                .Select(user =>
                {
                    names.TryGetValue(user.CountryId ?? Guid.Empty, out var country);
                    return new GetListQueryResponse(user.Id, user.Name ?? "", user.UserName ?? "", UserRoles.Name(user.RoleId), user.CountryId, country?.Name, country?.Dialect);
                })
                .ToList();
            return new Result<List<GetListQueryResponse>>(System.Net.HttpStatusCode.OK, rows, null);
        }
    }
}
