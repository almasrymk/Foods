namespace Foods.Application.MasterData.Content.ContentCategory.Queries.GetList
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using System.Collections.Generic;
    using Foods.Domain.Entity.MasterData.Content;
    using Foods.Application.Abstraction.QueryList;

    internal class GetListQueryHandler(IRepository<ContentGroup> _IRepository) : IQueryListHandler<GetListQuery, List<GetListQueryResponse>>
    {
        public Task<List<Result<List<GetListQueryResponse>>>> Handle(GetListQuery request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}