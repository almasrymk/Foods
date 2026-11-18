namespace Foods.Application.MasterData.Content.ContentGroup.Queries.GetById
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData.Content;

    public class GetByIdQueryHandler(IRepository<ContentGroup> _Repository) : IQueryHandler<GetByIdQuery, GetByIdQueryResponse>
    {
        public async Task<Result<GetByIdQueryResponse>> Handle(GetByIdQuery request, CancellationToken cancellationToken)
        {
            var Ob = await _Repository.GetByFilterAsync(e => e.Id == request.Id);

            if (Ob == null)
                Ob = new ContentGroup();

            return new Result<GetByIdQueryResponse>(System.Net.HttpStatusCode.OK,
                new GetByIdQueryResponse(Ob.Id, Ob.Name!, Ob.IconPath, Ob.Color, Ob.TextColor)
                , null);
        }
    }
}