namespace Foods.Application.MasterData.Item.Item.Commands.Update
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Entity.MasterData.Item;

    public sealed class UpdateCommandHandler(IUnitOfWork _UnitOfWork, IRepository<Item> _Repository, Foods.Domain.Abstraction.IFoodsDbContext db) : ICommandHandler<UpdateCommand, UpdateCommandResponse>
    {
        public async Task<Result<UpdateCommandResponse>> Handle(UpdateCommand request, CancellationToken cancellationToken)
        {
            var ob = await _Repository.GetByFilterAsync(e => e.Id == request.Id);
            if (ob == null)
                return new Result<UpdateCommandResponse>(
                    System.Net.HttpStatusCode.InternalServerError,
                    null,
                    new List<string> { "Error"});

            ob.Name = request.Name;
            ob.ItemCategoryId = request.ItemCategoryId;
            ob.CuisineId = request.CuisineId;
            ob.ItemGroupId = request.ItemGroupId;
            ob.Summary = request.Summary;
            ob.Description = request.Description;
            ob.ImagePath = request.ImagePath;
            ob.ShowToCustomer = request.ShowToCustomer;
            ob.Sort = request.Sort;
            await Foods.Application.MasterData.Item.Item.ItemCategories.Save(db, ob.Id, ob.ItemCategoryId, request.CategoryIds, cancellationToken);
            ob.IsDeleted = false;
            ob.Status = 0;

            var res = await _Repository.UpdateAsync(ob);
            if (await _UnitOfWork.SaveChangeAsync() > 0)
            {
                return new Result<UpdateCommandResponse>(
                    System.Net.HttpStatusCode.OK,
                    new UpdateCommandResponse(ob.Id),
                    null);
            }

            return new Result<UpdateCommandResponse>(
                   System.Net.HttpStatusCode.InternalServerError,
                   null,
                   new List<string> { "Error" });
        }
    }
}