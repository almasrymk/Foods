namespace Foods.Application.MasterData.Country.Queries.GetList
{
    using System.Net;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Shared;
    using CountryEntity = Foods.Domain.Entity.MasterData.Country;

    public sealed class GetListQueryHandler(IRepository<CountryEntity> repository) : IQueryHandler<GetListQuery, List<GetListQueryResponse>>
    {
        public async Task<Result<List<GetListQueryResponse>>> Handle(GetListQuery request, CancellationToken cancellationToken)
        {
            var list = await repository.GetListByFilterAsync() ?? new List<CountryEntity>();
            var rows = list
                .OrderBy(e => e.Name)
                .Select(e => new GetListQueryResponse(e.Id, e.Name ?? "", e.Dialect, e.IconPath, e.Color, e.TextColor, e.Currency, e.Neighbors))
                .ToList();
            return new Result<List<GetListQueryResponse>>(HttpStatusCode.OK, rows, null);
        }
    }
}
