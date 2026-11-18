namespace Foods.Application.MasterData.Country.Commands.Create
{
    using System.Net;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Shared;
    using CountryEntity = Foods.Domain.Entity.MasterData.Country;

    public sealed class CreateCommandHandler(IUnitOfWork unitOfWork, IRepository<CountryEntity> repository) : ICommandHandler<CreateCommand, CreateCommandResponse>
    {
        public async Task<Result<CreateCommandResponse>> Handle(CreateCommand request, CancellationToken cancellationToken)
        {
            var country = new CountryEntity
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Dialect = string.IsNullOrWhiteSpace(request.Dialect) ? null : request.Dialect.Trim(),
                IconPath = request.IconPath,
                Color = request.Color,
                TextColor = request.TextColor,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? null : request.Currency.Trim(),
                Neighbors = string.IsNullOrWhiteSpace(request.Neighbors) ? null : request.Neighbors.Trim(),
                IsDeleted = false,
                Status = 0
            };
            await repository.CreateAsync(country);
            if (await unitOfWork.SaveChangeAsync() <= 0)
                return new Result<CreateCommandResponse>(HttpStatusCode.InternalServerError, null, new List<string> { "Error" });

            return new Result<CreateCommandResponse>(HttpStatusCode.OK, new CreateCommandResponse(country.Id), null);
        }
    }
}
