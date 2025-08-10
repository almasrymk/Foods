namespace Foods.Application.MasterData.Content.ContentAlternative.Queries.GetList
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using System.Collections.Generic;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Content;

    public class GetListQueryHandler(IRepository<ContentAlternative> _Repository) : IQueryListHandler<GetListQuery, List<GetListQueryResponse>>
    { 
        public async Task<Result<List<GetListQueryResponse>>> Handle(GetListQuery request, CancellationToken cancellationToken)
        {
            var list = await _Repository.GetListByFilterAsync(e => 
            (e.ContentId == request.ContentId || request.ContentId == null) &&
            (e.ContentAlternativeId == request.ContentAlternativeId || request.ContentAlternativeId == null) 
            );

            if (list == null)
                list = new List<ContentAlternative>();

            return new Result<List<GetListQueryResponse>>(System.Net.HttpStatusCode.OK,
                list.Select(e=> new GetListQueryResponse(
                    e.Id,
                    e.ContentId , 
                    e.ContentAlternativeId)).ToList(), null);
        }
    }
}