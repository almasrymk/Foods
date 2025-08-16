namespace Foods.Application.MasterData.Item.ItemMeal.Queries.Search
{
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Item;

    public class SearchQueryHandler(IRepository<ItemMeal> _Repository) : IQueryHandler<SearchQuery, List<SearchQueryResponse>>
    {
        public async Task<Result<List<SearchQueryResponse>>> Handle(SearchQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync(e =>
                (e.ItemId == request.ItemId || "" + request.ItemId == "") &&
                (e.MealId == request.MealId || "" + request.MealId == ""));

            if (list == null)
                list = new List<ItemMeal>();

            return new Result<List<SearchQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.Select(e => new SearchQueryResponse(
                    e.Id,
                    e.ItemId,
                    e.MealId)).ToList() , null);
        }
    }
}