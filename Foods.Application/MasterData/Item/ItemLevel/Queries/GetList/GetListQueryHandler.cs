namespace Foods.Application.MasterData.Item.ItemLevel.Queries.GetList
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using System.Collections.Generic;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Item;

    public class GetListQueryHandler(IRepository<ItemLevel> _Repository) : IQueryListHandler<GetListQuery, List<GetListQueryResponse>>
    { 
        public async Task<Result<List<GetListQueryResponse>>> Handle(GetListQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync();

            if (list == null)
                list = new List<ItemLevel>();

            return new Result<List<GetListQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.Select(e=> new GetListQueryResponse(
                    e.Id,
                    e.ItemId,
                    e.LevelId)).ToList()
                , null);
        }
    }
}