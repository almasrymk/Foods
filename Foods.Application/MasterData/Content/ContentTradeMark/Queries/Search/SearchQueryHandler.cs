namespace Foods.Application.MasterData.Content.ContentTradeMark.Queries.Search
{
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Content;

    public class SearchQueryHandler(IRepository<ContentTradeMark> _Repository) : IQueryHandler<SearchQuery, List<SearchQueryResponse>>
    {
        public async Task<Result<List<SearchQueryResponse>>> Handle(SearchQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync(e =>
            (e.ContentId == request.ContentId || request.ContentId == null) &&
            (e.TradeMarkId == request.TradeMarkId || request.TradeMarkId == null)
            );

            if (list == null)
                list = new List<ContentTradeMark>();

            return new Result<List<SearchQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.Select(e => new SearchQueryResponse(
                    e.Id,
                    e.ContentId,
                    e.TradeMarkId)).ToList(), null);
        }
    }
}