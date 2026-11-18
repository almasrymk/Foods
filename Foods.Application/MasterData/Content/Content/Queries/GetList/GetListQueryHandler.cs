namespace Foods.Application.MasterData.Content.Content.Queries.GetList
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using System.Collections.Generic;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Content;

    public class GetListQueryHandler(IRepository<Content> _Repository) : IQueryListHandler<GetListQuery, List<GetListQueryResponse>>
    {
        public async Task<Result<List<GetListQueryResponse>>> Handle(GetListQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync(e=>
            (e.ContentCategoryId == request.ContentCategoryId || request.ContentCategoryId == null) &&
            (e.ContentGroupId == request.ContentGroupId || request.ContentGroupId == null) 
            );

            if (list == null)
                list = new List<Content>();

            return new Result<List<GetListQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.OrderBy(e => e.Sort).ThenBy(e => e.Name).Select(e => new GetListQueryResponse(
                    e.Id,
                    e.Name!,
                    e.ContentCategoryId,
                    e.ContentGroupId,
                    e.UnitId,
                    e.MiniQuantity,
                    e.ImagePath,
                    e.PurchasePlaceId,
                    e.PurchaseMode,
                    e.PackageName,
                    e.PurchaseStep,
                    e.PurchaseIncrement,
                    e.PurchaseUnitPrice,
                    e.PurchaseMinAmount,
                    e.Price,
                    e.PriceQuantity,
                    e.PriceUnitId,
                    e.CuisineId,
                    e.Sort
                    )).ToList(), null);
        }
    }
}