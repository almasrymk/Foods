namespace Foods.Application.MasterData.TradeMark.TradeMarkContentGroup.Queries.GetList
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using System.Collections.Generic;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.TradeMark;

    public class GetListQueryHandler(IRepository<TradeMarkContentGroup> _Repository) : IQueryListHandler<GetListQuery, List<GetListQueryResponse>>
    { 
        public async Task<Result<List<GetListQueryResponse>>> Handle(GetListQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync();

            if (list == null)
                list = new List<TradeMarkContentGroup>();

            return new Result<List<GetListQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.Select(e=> new GetListQueryResponse(e.Id, e.Name!, e.TradeMarkId, e.ContentGroupId)).ToList()
                , null);
        }
    }
}