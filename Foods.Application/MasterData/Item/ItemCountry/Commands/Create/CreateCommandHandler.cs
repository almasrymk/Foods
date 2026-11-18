namespace Foods.Application.MasterData.Item.ItemCountry.Commands.Create
{
    using System.Net;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData.Item;
    using Foods.Domain.Shared;

    public sealed class CreateCommandHandler(IUnitOfWork unitOfWork, IRepository<ItemCountry> repository) : ICommandHandler<CreateCommand, CreateCommandResponse>
    {
        public async Task<Result<CreateCommandResponse>> Handle(CreateCommand request, CancellationToken cancellationToken)
        {
            var prepared = await ItemCountryRules.Prepare(repository, request.ItemId, request.CountryId, null, request.IsPrimary);
            if (prepared.Error != null)
                return new Result<CreateCommandResponse>(HttpStatusCode.BadRequest, null, new List<string> { prepared.Error });

            var link = new ItemCountry
            {
                Id = Guid.NewGuid(),
                ItemId = request.ItemId,
                CountryId = request.CountryId,
                IsPrimary = prepared.IsPrimary,
                Name = Clean(request.Name),
                Summary = Clean(request.Summary),
                Description = Clean(request.Description),
                IsDeleted = false,
                Status = 0
            };
            await repository.CreateAsync(link);
            if (await unitOfWork.SaveChangeAsync() <= 0)
                return new Result<CreateCommandResponse>(HttpStatusCode.InternalServerError, null, new List<string> { "Error" });

            return new Result<CreateCommandResponse>(HttpStatusCode.OK, new CreateCommandResponse(link.Id), null);
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
