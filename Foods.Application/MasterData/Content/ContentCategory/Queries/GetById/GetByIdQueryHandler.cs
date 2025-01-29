namespace Foods.Application.MasterData.Content.ContentCategory.Queries.GetById
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Content;

    public class GetByIdQueryHandler(IRepository<ContentCategory> _Repository) : IQueryHandler<GetByIdQuery, GetByIdQueryResponse>
    {
        public async Task<Result<GetByIdQueryResponse>> Handle(GetByIdQuery request, CancellationToken cancellationToken)
        {
            var Ob = await _Repository.GetByFilterAsync(e => e.Id == request.Id);

            if (Ob == null)
                Ob = new ContentCategory();

            return new Result<GetByIdQueryResponse>(System.Net.HttpStatusCode.OK,
                new GetByIdQueryResponse(Ob.Id , Ob.Name!)
                , null);
        }
    }
}