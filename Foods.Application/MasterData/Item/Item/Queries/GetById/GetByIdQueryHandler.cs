namespace Foods.Application.MasterData.Item.Item.Queries.GetById
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Item;

    public class GetByIdQueryHandler(IRepository<Item> _Repository, IFoodsDbContext db) : IQueryHandler<GetByIdQuery, GetByIdQueryResponse>
    {
        public async Task<Result<GetByIdQueryResponse>> Handle(GetByIdQuery request, CancellationToken cancellationToken)
        {
            var Ob = await _Repository.GetByFilterAsync(e => e.Id == request.Id);

            if (Ob == null)
                Ob = new Item();

            var extra = await Foods.Application.MasterData.Item.Item.ItemCategories.Of(db, new[] { Ob.Id }, cancellationToken);
            return new Result<GetByIdQueryResponse>(System.Net.HttpStatusCode.OK,
                new GetByIdQueryResponse(Ob.Id , Ob.Name! , Ob.ItemCategoryId , Ob.CuisineId , Ob.ItemGroupId , Ob.Summary , Ob.Description, Ob.ImagePath, Ob.ShowToCustomer,
                    Ob.Sort, extra.TryGetValue(Ob.Id, out var ids) ? ids : new List<Guid>())
                , null);
        }
    }
}