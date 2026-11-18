namespace Foods.Application.MasterData.Cuisine.Commands.Delete
{
    using System.Net;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Entity.MasterData;

    public sealed class DeleteCommandHandler(IUnitOfWork _UnitOfWork, IRepository<Cuisine> _Repository) : ICommandHandler<DeleteCommand>
    {
        public async Task<Result> Handle(DeleteCommand request, CancellationToken cancellationToken)
        {
            var ob = await _Repository.GetByFilterAsync(e => e.Id == request.Id);
            if (ob == null)
                return new Result(HttpStatusCode.InternalServerError, new List<string> { "Error" });

            await _Repository.DeleteAsync(e => e.Id == request.Id);
            if (await _UnitOfWork.SaveChangeAsync() > 0)
                return new Result(HttpStatusCode.OK, null);

            return new Result(HttpStatusCode.InternalServerError, new List<string> { "Error" });
        }
    }
}
