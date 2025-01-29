namespace Foods.Application.MasterData.Content.ContentLevel.Commands.Update
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Entity.MasterData.Content;

    public sealed class UpdateCommandHandler(IUnitOfWork _UnitOfWork, IRepository<ContentLevel> _Repository) : ICommandHandler<UpdateCommand, UpdateCommandResponse>
    {
        public async Task<Result<UpdateCommandResponse>> Handle(UpdateCommand request, CancellationToken cancellationToken)
        {
            var ob = await _Repository.GetByFilterAsync(e => e.Id == request.Id);
            if (ob == null)
                return new Result<UpdateCommandResponse>(
                    System.Net.HttpStatusCode.InternalServerError,
                    null,
                    new List<string> { "Error"});

            ob.ContentId = request.ContentId;
            ob.LevelId = request.LevelId;

            var res = await _Repository.UpdateAsync(ob);
            if (await _UnitOfWork.SaveChangeAsync() > 0)
            {
                return new Result<UpdateCommandResponse>(
                    System.Net.HttpStatusCode.OK,
                    new UpdateCommandResponse(request.Id , request.ContentId , request.LevelId),
                    null);
            }

            return new Result<UpdateCommandResponse>(
                   System.Net.HttpStatusCode.InternalServerError,
                   null,
                   new List<string> { "Error" });
        }
    }
}