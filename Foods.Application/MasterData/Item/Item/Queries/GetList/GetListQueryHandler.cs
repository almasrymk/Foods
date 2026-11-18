namespace Foods.Application.MasterData.Item.Item.Queries.GetList
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using System.Collections.Generic;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Item;

    public class GetListQueryHandler(IRepository<Item> _Repository, IFoodsDbContext db) : IQueryListHandler<GetListQuery, List<GetListQueryResponse>>
    { 
        public async Task<Result<List<GetListQueryResponse>>> Handle(GetListQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync();

            if (list == null)
                list = new List<Item>();

            var extra = await Foods.Application.MasterData.Item.Item.ItemCategories.Of(db, list.Select(e => e.Id).ToList(), cancellationToken);
            return new Result<List<GetListQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.OrderBy(e => e.Sort).ThenBy(e => e.Name)
                    .Select(e => new GetListQueryResponse(e.Id, e.Name!, e.ImagePath, e.ItemCategoryId, e.CuisineId, e.ItemGroupId, e.Summary, e.ShowToCustomer,
                        e.Sort, extra.TryGetValue(e.Id, out var ids) ? ids : new List<Guid>())).ToList()
                , null);
        }
    }
}