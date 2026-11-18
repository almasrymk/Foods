namespace Foods.Application.MasterData.Unit.Queries.Search
{
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData;
    using Foods.Application.Abstraction.Query;  

    public class SearchQueryHandler(IRepository<Unit> _Repository) : IQueryHandler<SearchQuery, List<SearchQueryResponse>>
    {
        public async Task<Result<List<SearchQueryResponse>>> Handle(SearchQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync(e => ("" + e.Name).ToLower().Contains(("" + request.TextSearch).ToLower()) || "" + request.TextSearch == "");

            if (list == null)
                list = new List<Unit>();

            return new Result<List<SearchQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.Select(e => new SearchQueryResponse(
                    e.Id,
                    e.Name!,
                    e.NickName,
                    e.BasicQuantity,
                    e.BeDivided,
                    e.IconPath,
                    e.Color,
                    e.TextColor)).ToList(), null);
        }
    }
}