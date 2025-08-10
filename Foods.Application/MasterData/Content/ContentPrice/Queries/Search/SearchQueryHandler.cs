namespace Foods.Application.MasterData.Content.ContentPrice.Queries.Search
{
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Content;

    public class SearchQueryHandler(IRepository<ContentPrice> _Repository) : IQueryHandler<SearchQuery, List<SearchQueryResponse>>
    {
        public async Task<Result<List<SearchQueryResponse>>> Handle(SearchQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync(e => e.ContentId == request.ContentId || request.ContentId == null);

            if (list == null)
                list = new List<ContentPrice>();

            return new Result<List<SearchQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.Select(e => new SearchQueryResponse(
                    e.Id,
                    e.ContentId,
                    e.Price,
                    e.IsCurrent)).ToList(), null);
        }
    }
}