namespace Foods.Application.MasterData.Cuisine.Commands.Create
{
    using System.Net;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Entity.MasterData;

    public sealed class CreateCommandHandler(IUnitOfWork _UnitOfWork, IRepository<Cuisine> _Repository) : ICommandHandler<CreateCommand, CreateCommandResponse>
    {
        public async Task<Result<CreateCommandResponse>> Handle(CreateCommand request, CancellationToken cancellationToken)
        {
            var ob = new Cuisine
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                IconPath = request.IconPath,
                ImagePath = request.ImagePath,
                CountryId = request.CountryId,
                Color = request.Color,
                TextColor = request.TextColor,
                Sort = request.Sort,
                IsDeleted = false,
                Status = 0
            };

            var res = await _Repository.CreateAsync(ob);
            if (await _UnitOfWork.SaveChangeAsync() > 0)
                return new Result<CreateCommandResponse>(HttpStatusCode.OK, new CreateCommandResponse(res.Id), null);

            return new Result<CreateCommandResponse>(HttpStatusCode.InternalServerError, null, new List<string> { "Error" });
        }
    }
}
