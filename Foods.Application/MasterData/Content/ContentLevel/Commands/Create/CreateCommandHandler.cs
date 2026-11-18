namespace Foods.Application.MasterData.Content.ContentLevel.Commands.Create
{
    using System.Net;
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Entity.MasterData.Content;

    public sealed class CreateCommandHandler(IUnitOfWork _UnitOfWork, IRepository<ContentLevel> _Repository) : ICommandHandler<CreateCommand, CreateCommandResponse>
    {
        public async Task<Result<CreateCommandResponse>> Handle(CreateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var ob = new ContentLevel
                {
                    Id = Guid.NewGuid(),
                    ContentId = request.ContentId,
                    LevelId = request.LevelId,
                    IsDeleted = false,
                    Status = 0
                };

                var res = await _Repository.CreateAsync(ob);
                if (await _UnitOfWork.SaveChangeAsync() > 0)
                {
                    return new Result<CreateCommandResponse>(
                        HttpStatusCode.OK,
                        new CreateCommandResponse(res.Id,res.ContentId , res.LevelId),
                        null);
                }

                return new Result<CreateCommandResponse>(
                    HttpStatusCode.InternalServerError,
                    null,
                    new List<string> { "Error" });
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}