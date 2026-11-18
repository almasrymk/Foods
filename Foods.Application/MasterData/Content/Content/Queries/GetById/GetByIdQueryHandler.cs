namespace Foods.Application.MasterData.Content.Content.Queries.GetById
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Content;

    public class GetByIdQueryHandler(IRepository<Content> _Repository) : IQueryHandler<GetByIdQuery, GetByIdQueryResponse>
    {
        public async Task<Result<GetByIdQueryResponse>> Handle(GetByIdQuery request, CancellationToken cancellationToken)
        {
            var Ob = await _Repository.GetByFilterAsync(e => e.Id == request.Id);

            if (Ob == null)
                Ob = new Content();

            return new Result<GetByIdQueryResponse>(System.Net.HttpStatusCode.OK,
                new GetByIdQueryResponse(
                    Ob.Id , 
                    Ob.Name! , 
                    Ob.ContentCategoryId , 
                    Ob.ContentGroupId,
                    Ob.UnitId ,
                    Ob.MiniQuantity,
                    Ob.ImagePath,
                    Ob.PurchasePlaceId,
                    Ob.PurchaseMode,
                    Ob.PackageName,
                    Ob.PurchaseStep,
                    Ob.PurchaseIncrement,
                    Ob.PurchaseUnitPrice,
                    Ob.PurchaseMinAmount,
                    Ob.Price,
                    Ob.PriceQuantity,
                    Ob.PriceUnitId,
                    Ob.CuisineId,
                    Ob.Sort
                    ) , null);
        }
    }
}