namespace Foods.Application.MasterData.Country.Queries.Search
{
    using System.Net;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Shared;
    using CountryEntity = Foods.Domain.Entity.MasterData.Country;

    public sealed class SearchQueryHandler(IRepository<CountryEntity> repository) : IQueryHandler<SearchQuery, List<SearchQueryResponse>>
    {
        public async Task<Result<List<SearchQueryResponse>>> Handle(SearchQuery request, CancellationToken cancellationToken)
        {
            var text = (request.TextSearch ?? "").Trim().ToLower();
            var list = await repository.GetListByFilterAsync(e =>
                text.Length == 0 ||
                ("" + e.Name).ToLower().Contains(text) ||
                ("" + e.Dialect).ToLower().Contains(text)) ?? new List<CountryEntity>();

            var rows = list
                .OrderBy(e => e.Name)
                .Select(e => new SearchQueryResponse(e.Id, e.Name ?? "", e.Dialect, e.IconPath, e.Color, e.TextColor, e.Currency, e.Neighbors))
                .ToList();
            return new Result<List<SearchQueryResponse>>(HttpStatusCode.OK, rows, null);
        }
    }
}
