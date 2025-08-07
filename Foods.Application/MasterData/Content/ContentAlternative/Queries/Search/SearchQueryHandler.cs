namespace Foods.Application.MasterData.Content.ContentAlternative.Queries.Search
{
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Content;

    public class SearchQueryHandler(IRepository<ContentAlternative> _Repository) : IQueryHandler<SearchQuery, List<SearchQueryResponse>>
    {
        public async Task<Result<List<SearchQueryResponse>>> Handle(SearchQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync(e => 1== 1/* ("" + e.Name).ToLower().Contains(("" + request.txtSearch).ToLower())*/);

            if (list == null)
                list = new List<ContentAlternative>();

            return new Result<List<SearchQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.Select(e => new SearchQueryResponse(e.Id, ""/*e.Name!*/)).ToList()
                , null);
        }
    }
}