namespace Foods.Application.MasterData.Item.ItemMeal.Commands.Update
{
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Entity.MasterData.Item;

    public sealed class UpdateCommandHandler(IUnitOfWork _UnitOfWork, IRepository<ItemMeal> _Repository) : ICommandHandler<UpdateCommand, UpdateCommandResponse>
    {
        public async Task<Result<UpdateCommandResponse>> Handle(UpdateCommand request, CancellationToken cancellationToken)
        {
            var ob = await _Repository.GetByFilterAsync(e => e.Id == request.Id);
            if (ob == null)
                return new Result<UpdateCommandResponse>(
                    System.Net.HttpStatusCode.InternalServerError,
                    null,
                    new List<string> { "Error"});

            ob.ItemId = request.ItemId;
            ob.MealId = request.MealId;
            ob.IsDeleted = false;
            ob.Status = 0;

            var res = await _Repository.UpdateAsync(ob);
            if (await _UnitOfWork.SaveChangeAsync() > 0)
            {
                return new Result<UpdateCommandResponse>(
                    System.Net.HttpStatusCode.OK,
                    new UpdateCommandResponse(request.Id),
                    null);
            }

            return new Result<UpdateCommandResponse>(
                   System.Net.HttpStatusCode.InternalServerError,
                   null,
                   new List<string> { "Error" });
        }
    }
}