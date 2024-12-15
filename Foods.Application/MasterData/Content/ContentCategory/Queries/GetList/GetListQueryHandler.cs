namespace Foods.Application.MasterData.Content.ContentCategory.Queries.GetList
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using System.Collections.Generic;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Content;

    internal class GetListQueryHandler(IRepository<ContentGroup> _IRepository) : IQueryListHandler<GetListQuery, GetListQueryResponse>
    {
        public async Task<List<Result<GetListQueryResponse>>> Handle(GetListQuery request, CancellationToken cancellationToken)
        {
            var list = await _IRepository.GetListByFilterAsync();
            return new List<Result<GetListQueryResponse>>();
        }
    }
}