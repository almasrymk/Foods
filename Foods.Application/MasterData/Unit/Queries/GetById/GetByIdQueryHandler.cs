namespace Foods.Application.MasterData.Unit.Queries.GetById
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData;

    public class GetByIdQueryHandler(IRepository<Unit> _Repository) : IQueryHandler<GetByIdQuery, GetByIdQueryResponse>
    {
        public async Task<Result<GetByIdQueryResponse>> Handle(GetByIdQuery request, CancellationToken cancellationToken)
        {
            var Ob = await _Repository.GetByFilterAsync(e => e.Id == request.Id);

            if (Ob == null)
                Ob = new Unit();

            return new Result<GetByIdQueryResponse>(System.Net.HttpStatusCode.OK,
                new GetByIdQueryResponse(
                    Ob.Id , 
                    Ob.Name!,
                    Ob.NickName,
                    Ob.BasicQuantity,
                    Ob.BeDivided), null);
        }
    }
}