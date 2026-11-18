namespace Foods.Application.MasterData.Item.ItemCountry.Queries.GetById
{
    using System.Net;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData.Item;
    using Foods.Domain.Shared;

    public sealed class GetByIdQueryHandler(IRepository<ItemCountry> repository) : IQueryHandler<GetByIdQuery, GetByIdQueryResponse>
    {
        public async Task<Result<GetByIdQueryResponse>> Handle(GetByIdQuery request, CancellationToken cancellationToken)
        {
            var link = await repository.GetByFilterAsync(e => e.Id == request.Id);
            if (link == null)
                return new Result<GetByIdQueryResponse>(HttpStatusCode.BadRequest, null, new List<string> { "ربط البلد غير موجود" });

            return new Result<GetByIdQueryResponse>(HttpStatusCode.OK, Map(link), null);
        }

        internal static GetByIdQueryResponse Map(ItemCountry link) =>
            new(link.Id, link.ItemId, link.CountryId, link.IsPrimary, link.Name, link.Summary, link.Description);
    }
}
