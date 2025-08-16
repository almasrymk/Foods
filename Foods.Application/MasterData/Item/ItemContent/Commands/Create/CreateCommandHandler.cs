namespace Foods.Application.MasterData.Item.ItemContent.Commands.Create
{
    using System.Net;
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Entity.MasterData.Item;

    public sealed class CreateCommandHandler(IUnitOfWork _UnitOfWork, IRepository<ItemContent> _Repository) : ICommandHandler<CreateCommand, CreateCommandResponse>
    {
        public async Task<Result<CreateCommandResponse>> Handle(CreateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var ob = new ItemContent
                {
                    Id = new Guid(),
                    ItemId = request.ItemId,
                    ContentId = request.ContentId,
                    UnitId = request.UnitId,
                    Quantity = request.Quantity,
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