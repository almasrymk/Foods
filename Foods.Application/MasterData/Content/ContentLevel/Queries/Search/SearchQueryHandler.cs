namespace Foods.Application.MasterData.Content.ContentLevel.Queries.Search
{
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Content;

    public class SearchQueryHandler(IRepository<ContentLevel> _Repository) : IQueryHandler<SearchQuery, List<SearchQueryResponse>>
    {
        public async Task<Result<List<SearchQueryResponse>>> Handle(SearchQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync(e => e.ContentId == request.ContentId || e.LevelId == request.LevelId);

            if (list == null)
                list = new List<ContentLevel>();

            return new Result<List<SearchQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.Select(e => new SearchQueryResponse(e.Id, e.ContentId , e.LevelId)).ToList()
                , null);
        }
    }
}