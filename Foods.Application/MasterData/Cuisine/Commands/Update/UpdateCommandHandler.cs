namespace Foods.Application.MasterData.Cuisine.Commands.Update
{
    using System.Net;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Entity.MasterData;

    public sealed class UpdateCommandHandler(IUnitOfWork _UnitOfWork, IRepository<Cuisine> _Repository) : ICommandHandler<UpdateCommand, UpdateCommandResponse>
    {
        public async Task<Result<UpdateCommandResponse>> Handle(UpdateCommand request, CancellationToken cancellationToken)
        {
            var ob = await _Repository.GetByFilterAsync(e => e.Id == request.Id);
            if (ob == null)
                return new Result<UpdateCommandResponse>(HttpStatusCode.InternalServerError, null, new List<string> { "Error" });

            ob.Name = request.Name;
            ob.IconPath = request.IconPath;
            ob.ImagePath = request.ImagePath;
            ob.CountryId = request.CountryId;
            ob.Color = request.Color;
            ob.TextColor = request.TextColor;
            ob.Sort = request.Sort;

            await _Repository.UpdateAsync(ob);
            if (await _UnitOfWork.SaveChangeAsync() > 0)
                return new Result<UpdateCommandResponse>(HttpStatusCode.OK, new UpdateCommandResponse(request.Id), null);

            return new Result<UpdateCommandResponse>(HttpStatusCode.InternalServerError, null, new List<string> { "Error" });
        }
    }
}
