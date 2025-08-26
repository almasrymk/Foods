namespace Foods.Application.MasterData.Item.ItemContent.Queries.GetList
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using System.Collections.Generic;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Item;

    public class GetListQueryHandler(IRepository<ItemContent> _Repository) : IQueryListHandler<GetListQuery, List<GetListQueryResponse>>
    { 
        public async Task<Result<List<GetListQueryResponse>>> Handle(GetListQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync(e =>
            (e.ItemId == request.ItemId || "" + request.ItemId == "") &&
            (e.ContentId == request.ContentId || "" + request.ContentId == "") &&
            (e.UnitId == request.UnitId || "" + request.UnitId == ""));

            if (list == null)
                list = new List<ItemContent>();

            return new Result<List<GetListQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.Select(e=> new GetListQueryResponse(
                    e.Id,
                    e.ItemId,
                    e.ContentId,
                    e.UnitId,
                    e.Quantity)).ToList()
                , null);
        }
    }
}