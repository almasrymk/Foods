namespace Foods.Application.MasterData.TradeMark.TradeMarkContentGroup.Queries.Search
{
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.TradeMark;

    public class SearchQueryHandler(IRepository<TradeMarkContentGroup> _Repository) : IQueryHandler<SearchQuery, List<SearchQueryResponse>>
    {
        public async Task<Result<List<SearchQueryResponse>>> Handle(SearchQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync(e => 
            (("" + e.Name).ToLower().Contains(("" + request.TextSearch).ToLower()) || request.TextSearch == "") &&
            (e.ContentGroupId == request.ContentGroupId || "" + request.ContentGroupId == "") &&
            (e.TradeMarkId == request.TradeMarkId || "" + request.TradeMarkId == ""));

            if (list == null)
                list = new List<TradeMarkContentGroup>();

            return new Result<List<SearchQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.Select(e => new SearchQueryResponse(e.Id, e.Name!, e.TradeMarkId, e.ContentGroupId)).ToList()
                , null);
        }
    }
}