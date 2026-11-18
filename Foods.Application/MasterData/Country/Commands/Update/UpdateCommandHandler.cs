namespace Foods.Application.MasterData.Country.Commands.Update
{
    using System.Net;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Shared;
    using CountryEntity = Foods.Domain.Entity.MasterData.Country;

    public sealed class UpdateCommandHandler(IUnitOfWork unitOfWork, IRepository<CountryEntity> repository) : ICommandHandler<UpdateCommand, UpdateCommandResponse>
    {
        public async Task<Result<UpdateCommandResponse>> Handle(UpdateCommand request, CancellationToken cancellationToken)
        {
            var country = await repository.GetByFilterAsync(e => e.Id == request.Id);
            if (country == null)
                return new Result<UpdateCommandResponse>(HttpStatusCode.BadRequest, null, new List<string> { "البلد غير موجودة" });

            country.Name = request.Name.Trim();
            country.Dialect = string.IsNullOrWhiteSpace(request.Dialect) ? null : request.Dialect.Trim();
            country.IconPath = request.IconPath;
            country.Color = request.Color;
            country.TextColor = request.TextColor;
            country.Currency = string.IsNullOrWhiteSpace(request.Currency) ? null : request.Currency.Trim();
            country.Neighbors = string.IsNullOrWhiteSpace(request.Neighbors) ? null : request.Neighbors.Trim();
            await repository.UpdateAsync(country);
            if (await unitOfWork.SaveChangeAsync() <= 0)
                return new Result<UpdateCommandResponse>(HttpStatusCode.InternalServerError, null, new List<string> { "Error" });

            return new Result<UpdateCommandResponse>(HttpStatusCode.OK, new UpdateCommandResponse(country.Id), null);
        }
    }
}
