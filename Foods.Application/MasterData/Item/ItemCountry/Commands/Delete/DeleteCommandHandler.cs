namespace Foods.Application.MasterData.Item.ItemCountry.Commands.Delete
{
    using System.Net;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData.Item;
    using Foods.Domain.Shared;

    public sealed class DeleteCommandHandler(IUnitOfWork unitOfWork, IRepository<ItemCountry> repository) : ICommandHandler<DeleteCommand>
    {
        public async Task<Result> Handle(DeleteCommand request, CancellationToken cancellationToken)
        {
            var link = await repository.GetByFilterAsync(e => e.Id == request.Id);
            if (link == null)
                return new Result(HttpStatusCode.BadRequest, new List<string> { "ربط البلد غير موجود" });

            if (link.IsPrimary)
            {
                var others = (await repository.GetListByFilterAsync(e => e.ItemId == link.ItemId && e.Id != link.Id))?.ToList() ?? new List<ItemCountry>();
                var next = others.FirstOrDefault();
                if (next != null)
                {
                    next.IsPrimary = true;
                    await repository.UpdateAsync(next);
                }
            }

            await repository.DeleteAsync(e => e.Id == request.Id);
            if (await unitOfWork.SaveChangeAsync() <= 0)
                return new Result(HttpStatusCode.InternalServerError, new List<string> { "Error" });

            return new Result(HttpStatusCode.OK, null);
        }
    }
}
