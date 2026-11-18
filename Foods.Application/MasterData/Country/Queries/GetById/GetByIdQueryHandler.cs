namespace Foods.Application.MasterData.Country.Queries.GetById
{
    using System.Net;
    using Foods.Application.Abstraction.Query;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Shared;
    using CountryEntity = Foods.Domain.Entity.MasterData.Country;

    public sealed class GetByIdQueryHandler(IRepository<CountryEntity> repository) : IQueryHandler<GetByIdQuery, GetByIdQueryResponse>
    {
        public async Task<Result<GetByIdQueryResponse>> Handle(GetByIdQuery request, CancellationToken cancellationToken)
        {
            var country = await repository.GetByFilterAsync(e => e.Id == request.Id);
            if (country == null)
                return new Result<GetByIdQueryResponse>(HttpStatusCode.BadRequest, null, new List<string> { "البلد غير موجودة" });

            return new Result<GetByIdQueryResponse>(HttpStatusCode.OK, new GetByIdQueryResponse(country.Id, country.Name ?? "", country.Dialect, country.IconPath, country.Color, country.TextColor, country.Currency, country.Neighbors), null);
        }
    }
}
