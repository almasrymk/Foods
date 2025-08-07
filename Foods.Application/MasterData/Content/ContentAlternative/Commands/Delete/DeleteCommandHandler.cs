namespace Foods.Application.MasterData.Content.ContentAlternative.Commands.Delete
{
    using System.Net;
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Entity.MasterData.Content;

    public sealed class DeleteCommandHandler(IUnitOfWork _UnitOfWork, IRepository<ContentAlternative> _Repository) : ICommandHandler<DeleteCommand>
    {        
        public async Task<Result> Handle(DeleteCommand request, CancellationToken cancellationToken)
        {
            var ob = await _Repository.GetByFilterAsync(e => e.Id == request.Id);
            if (ob == null)
                return new Result(
                     HttpStatusCode.InternalServerError,
                     new List<string> { "Error" });

            var res = await _Repository.DeleteAsync(e => e.Id == request.Id);
            if (await _UnitOfWork.SaveChangeAsync() > 0)
            {
                return new Result(System.Net.HttpStatusCode.OK, null);
            }

            return new Result(
                  HttpStatusCode.InternalServerError,
                  new List<string> { "Error" });
        }
    }
}