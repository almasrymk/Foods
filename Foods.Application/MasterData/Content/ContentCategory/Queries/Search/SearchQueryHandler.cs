namespace Foods.Application.MasterData.Content.ContentCategory.Queries.Search
{
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Content;

    public class SearchQueryHandler(IRepository<ContentCategory> _Repository) : IQueryHandler<SearchQuery, List<SearchQueryResponse>>
    {
        public async Task<Result<List<SearchQueryResponse>>> Handle(SearchQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync(e => ("" + e.Name).ToLower().Contains(("" + request.TextSearch).ToLower()) || "" + request.TextSearch == "");

            if (list == null)
                list = new List<ContentCategory>();

            return new Result<List<SearchQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.Select(e => new SearchQueryResponse(e.Id, e.Name!, e.IconPath, e.Color, e.TextColor)).ToList()
                , null);
        }
    }
}