namespace Foods.Application.MasterData.Item.ItemMeal.Queries.GetList
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using System.Collections.Generic;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Item;

    public class GetListQueryHandler(IRepository<ItemMeal> _Repository) : IQueryListHandler<GetListQuery, List<GetListQueryResponse>>
    {
        public async Task<Result<List<GetListQueryResponse>>> Handle(GetListQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync();

            if (list == null)
                list = new List<ItemMeal>();

            return new Result<List<GetListQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.Select(e => new GetListQueryResponse(
                    e.Id,
                    e.ItemId,
                    e.MealId)).ToList()
                , null);
        }
    }
}