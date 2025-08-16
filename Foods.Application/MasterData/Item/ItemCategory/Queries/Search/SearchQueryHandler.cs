namespace Foods.Application.MasterData.Item.ItemCategory.Queries.Search
{
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Item;

    public class SearchQueryHandler(IRepository<ItemCategory> _Repository) : IQueryHandler<SearchQuery, List<SearchQueryResponse>>
    {
        public async Task<Result<List<SearchQueryResponse>>> Handle(SearchQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync(e => ("" + e.Name).ToLower().Contains(("" + request.TextSearch).ToLower()) || "" + request.TextSearch == "");

            if (list == null)
                list = new List<ItemCategory>();

            return new Result<List<SearchQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.Select(e => new SearchQueryResponse(e.Id, e.Name!)).ToList()
                , null);
        }
    }
}