namespace Foods.Application.MasterData.Item.ItemGroup.Queries.Search
{
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Item;

    public class SearchQueryHandler(IRepository<ItemGroup> _Repository) : IQueryHandler<SearchQuery, List<SearchQueryResponse>>
    {
        public async Task<Result<List<SearchQueryResponse>>> Handle(SearchQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync(e => ("" + e.Name).ToLower().Contains(("" + request.TextSearch).ToLower()) || "" + request.TextSearch == "");

            if (list == null)
                list = new List<ItemGroup>();

            return new Result<List<SearchQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.OrderBy(e => e.Sort).ThenBy(e => e.Name).Select(e => new SearchQueryResponse(e.Id, e.Name!, e.IconPath, e.Color, e.TextColor, e.Sort)).ToList()
                , null);
        }
    }
}