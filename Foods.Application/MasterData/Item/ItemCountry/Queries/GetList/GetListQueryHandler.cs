namespace Foods.Application.MasterData.Item.ItemCountry.Queries.GetList
{
    using System.Net;
    using Foods.Application.Abstraction.Query;
    using Foods.Application.MasterData.Item.ItemCountry.Queries.GetById;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData.Item;
    using Foods.Domain.Shared;

    public sealed class GetListQueryHandler(IRepository<ItemCountry> repository) : IQueryHandler<GetListQuery, List<GetByIdQueryResponse>>
    {
        public async Task<Result<List<GetByIdQueryResponse>>> Handle(GetListQuery request, CancellationToken cancellationToken)
        {
            var list = await repository.GetListByFilterAsync(e =>
                (request.ItemId == null || e.ItemId == request.ItemId) &&
                (request.CountryId == null || e.CountryId == request.CountryId)) ?? new List<ItemCountry>();

            var rows = list
                .OrderByDescending(e => e.IsPrimary)
                .ThenBy(e => e.Name)
                .Select(GetByIdQueryHandler.Map)
                .ToList();
            return new Result<List<GetByIdQueryResponse>>(HttpStatusCode.OK, rows, null);
        }
    }
}
