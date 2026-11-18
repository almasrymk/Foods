namespace Foods.Application.MasterData.Item.ItemCategory.Queries.GetList
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using System.Collections.Generic;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Item;

    public class GetListQueryHandler(IRepository<ItemCategory> _Repository) : IQueryListHandler<GetListQuery, List<GetListQueryResponse>>
    {
        public async Task<Result<List<GetListQueryResponse>>> Handle(GetListQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync();

            if (list == null)
                list = new List<ItemCategory>();

            return new Result<List<GetListQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.OrderBy(e => e.Sort).ThenBy(e => e.Name).Select(e => new GetListQueryResponse(e.Id, e.Name!, e.IconPath, e.Color, e.TextColor, e.Sort)).ToList()
                , null);
        }
    }
}
