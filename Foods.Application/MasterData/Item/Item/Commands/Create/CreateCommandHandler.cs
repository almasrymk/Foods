namespace Foods.Application.MasterData.Item.Item.Commands.Create
{
    using System.Net;
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Entity.MasterData.Item;

    public sealed class CreateCommandHandler(IUnitOfWork _UnitOfWork, IRepository<Item> _Repository, IFoodsDbContext db) : ICommandHandler<CreateCommand, CreateCommandResponse>
    {
        public async Task<Result<CreateCommandResponse>> Handle(CreateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var ob = new Item
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    ItemCategoryId = request.ItemCategoryId,
                    CuisineId = request.CuisineId,
                    ItemGroupId = request.ItemGroupId,
                    Description = request.Description,
                    Summary = request.Summary,
                    ImagePath = request.ImagePath,
                    ShowToCustomer = request.ShowToCustomer,
                    Sort = request.Sort,
                    IsDeleted = false,
                    Status = 0
                };

                var res = await _Repository.CreateAsync(ob);
                await ItemCategories.Save(db, ob.Id, ob.ItemCategoryId, request.CategoryIds, cancellationToken);
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