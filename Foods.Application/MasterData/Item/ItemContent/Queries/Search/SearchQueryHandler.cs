namespace Foods.Application.MasterData.Item.ItemContent.Queries.Search
{
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Item;

    public class SearchQueryHandler(IRepository<ItemContent> _Repository) : IQueryHandler<SearchQuery, List<SearchQueryResponse>>
    {
        public async Task<Result<List<SearchQueryResponse>>> Handle(SearchQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync(e => 
            (e.ItemId == request.ItemId || "" + request.ItemId == "") && 
            (e.ContentId == request.ContentId || "" + request.ContentId == "") && 
            (e.UnitId == request.UnitId || "" + request.UnitId == ""));

            if (list == null)
                list = new List<ItemContent>();

            return new Result<List<SearchQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.Select(e => new SearchQueryResponse(
                    e.Id,
                    e.ItemId,
                    e.ContentId,
                    e.UnitId,
                    e.Quantity)).ToList()
                , null);
        }
    }
}