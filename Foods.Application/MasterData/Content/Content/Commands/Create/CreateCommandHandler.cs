namespace Foods.Application.MasterData.Content.Content.Commands.Create
{
    using System.Net;
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Entity.MasterData.Content;

    public sealed class CreateCommandHandler(IUnitOfWork _UnitOfWork, IRepository<Content> _Repository) : ICommandHandler<CreateCommand, CreateCommandResponse>
    {
        public async Task<Result<CreateCommandResponse>> Handle(CreateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var ob = new Content
                {
                    Id = new Guid(),
                    Name = request.Name,
                    ContentCategoryId = request.ContentCategoryId,
                    ContentGroupId = request.ContentGroupId,
                    MiniQuantity = request.MiniQuantity,
                    UnitId = request.UnitId,
                    IsDeleted = false,
                    Status = 0                    
                };

                var res = await _Repository.CreateAsync(ob);
                if (await _UnitOfWork.SaveChangeAsync() > 0)
                {
                    return new Result<CreateCommandResponse>(
                        HttpStatusCode.OK,
                        new CreateCommandResponse(res.Id),
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