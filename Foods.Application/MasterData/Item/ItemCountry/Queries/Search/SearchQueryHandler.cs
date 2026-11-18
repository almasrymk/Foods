namespace Foods.Application.MasterData.Item.ItemCountry.Queries.Search
{
    using System.Net;
    using Foods.Application.Abstraction.Query;
    using Foods.Application.MasterData.Item.ItemCountry.Queries.GetById;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData.Item;
    using Foods.Domain.Shared;

    public sealed class SearchQueryHandler(IRepository<ItemCountry> repository) : IQueryHandler<SearchQuery, List<GetByIdQueryResponse>>
    {
        public async Task<Result<List<GetByIdQueryResponse>>> Handle(SearchQuery request, CancellationToken cancellationToken)
        {
            var list = await repository.GetListByFilterAsync(e =>
                (request.ItemId == null || e.ItemId == request.ItemId) &&
                (request.CountryId == null || e.CountryId == request.CountryId)) ?? new List<ItemCountry>();

            return new Result<List<GetByIdQueryResponse>>(HttpStatusCode.OK, list.Select(GetByIdQueryHandler.Map).ToList(), null);
        }
    }
}
