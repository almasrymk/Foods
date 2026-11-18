namespace Foods.Application.MasterData.Cuisine.Queries.GetById
{
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Entity.MasterData;

    public class GetByIdQueryHandler(IRepository<Cuisine> _Repository) : IQueryHandler<GetByIdQuery, GetByIdQueryResponse>
    {
        public async Task<Result<GetByIdQueryResponse>> Handle(GetByIdQuery request, CancellationToken cancellationToken)
        {
            var ob = await _Repository.GetByFilterAsync(e => e.Id == request.Id) ?? new Cuisine();
            return new Result<GetByIdQueryResponse>(System.Net.HttpStatusCode.OK,
                new GetByIdQueryResponse(ob.Id, ob.Name!, ob.IconPath, ob.Color, ob.TextColor, ob.Sort, ob.ImagePath, ob.CountryId),
                null);
        }
    }
}
