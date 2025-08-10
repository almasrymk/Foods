namespace Foods.Application.MasterData.Content.ContentTradeMark.Queries.GetList
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using System.Collections.Generic;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Content;

    public class GetListQueryHandler(IRepository<ContentTradeMark> _Repository) : IQueryListHandler<GetListQuery, List<GetListQueryResponse>>
    { 
        public async Task<Result<List<GetListQueryResponse>>> Handle(GetListQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync(e => 
            (e.ContentId == request.ContentId || request.ContentId == null) &&
            (e.TradeMarkId == request.TradeMarkId || request.TradeMarkId == null) 
            );

            if (list == null)
                list = new List<ContentTradeMark>();

            return new Result<List<GetListQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.Select(e => new GetListQueryResponse(
                    e.Id,
                    e.ContentId,
                    e.TradeMarkId)).ToList(), null);
        }
    }
}