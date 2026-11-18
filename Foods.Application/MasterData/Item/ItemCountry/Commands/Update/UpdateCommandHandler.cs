namespace Foods.Application.MasterData.Item.ItemCountry.Commands.Update
{
    using System.Net;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData.Item;
    using Foods.Domain.Shared;

    public sealed class UpdateCommandHandler(IUnitOfWork unitOfWork, IRepository<ItemCountry> repository) : ICommandHandler<UpdateCommand, UpdateCommandResponse>
    {
        public async Task<Result<UpdateCommandResponse>> Handle(UpdateCommand request, CancellationToken cancellationToken)
        {
            var link = await repository.GetByFilterAsync(e => e.Id == request.Id);
            if (link == null)
                return new Result<UpdateCommandResponse>(HttpStatusCode.BadRequest, null, new List<string> { "ربط البلد غير موجود" });

            var prepared = await ItemCountryRules.Prepare(repository, request.ItemId, request.CountryId, link.Id, request.IsPrimary);
            if (prepared.Error != null)
                return new Result<UpdateCommandResponse>(HttpStatusCode.BadRequest, null, new List<string> { prepared.Error });

            link.ItemId = request.ItemId;
            link.CountryId = request.CountryId;
            link.IsPrimary = prepared.IsPrimary;
            link.Name = Clean(request.Name);
            link.Summary = Clean(request.Summary);
            link.Description = Clean(request.Description);
            await repository.UpdateAsync(link);
            if (await unitOfWork.SaveChangeAsync() <= 0)
                return new Result<UpdateCommandResponse>(HttpStatusCode.InternalServerError, null, new List<string> { "Error" });

            return new Result<UpdateCommandResponse>(HttpStatusCode.OK, new UpdateCommandResponse(link.Id), null);
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
