namespace Foods.Application.MasterData.Cuisine.Queries.Search
{
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData;

    public class SearchQueryHandler(IRepository<Cuisine> _Repository) : IQueryHandler<SearchQuery, List<SearchQueryResponse>>
    {
        public async Task<Result<List<SearchQueryResponse>>> Handle(SearchQuery request, CancellationToken cancellationToken)
        {
            var text = (request.TextSearch ?? "").Trim().ToLower();
            var list = await _Repository.GetListByFilterAsync(e => ("" + e.Name).ToLower().Contains(text)) ?? new List<Cuisine>();
            return new Result<List<SearchQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.OrderBy(e => e.Sort).ThenBy(e => e.Name)
                    .Select(e => new SearchQueryResponse(e.Id, e.Name!, e.IconPath, e.Color, e.TextColor, e.Sort, e.ImagePath, e.CountryId))
                    .ToList(),
                null);
        }
    }
}
