namespace Foods.Application.MasterData.Meal.Queries.GetList
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using System.Collections.Generic;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData;

    public class GetListQueryHandler(IRepository<Meal> _Repository) : IQueryListHandler<GetListQuery, List<GetListQueryResponse>>
    { 
        public async Task<Result<List<GetListQueryResponse>>> Handle(GetListQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync();

            if (list == null)
                list = new List<Meal>();

            return new Result<List<GetListQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.OrderBy(e => e.Sort).ThenBy(e => e.Name)
                    .Select(e => new GetListQueryResponse(e.Id, e.Name!, e.Description!, e.ImagePath, e.IconPath, e.Color, e.TextColor, e.Sort))
                    .ToList()
                , null);
        }
    }
}