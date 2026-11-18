namespace Foods.Application.MasterData.Country.Commands.Delete
{
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Foods.Application.Abstraction.Command;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData.Item;
    using Foods.Domain.Entity.Security;
    using Foods.Domain.Shared;
    using CountryEntity = Foods.Domain.Entity.MasterData.Country;

    public sealed class DeleteCommandHandler(IUnitOfWork unitOfWork, IRepository<CountryEntity> repository, IFoodsDbContext db) : ICommandHandler<DeleteCommand>
    {
        public async Task<Result> Handle(DeleteCommand request, CancellationToken cancellationToken)
        {
            var country = await repository.GetByFilterAsync(e => e.Id == request.Id);
            if (country == null)
                return new Result(HttpStatusCode.BadRequest, new List<string> { "البلد غير موجودة" });

            var usedByUser = await db.Set<User>().AnyAsync(e => e.CountryId == request.Id && !e.IsDeleted, cancellationToken);
            var usedByItem = await db.Set<ItemCountry>().AnyAsync(e => e.CountryId == request.Id && !e.IsDeleted, cancellationToken);
            if (usedByUser || usedByItem)
                return new Result(HttpStatusCode.BadRequest, new List<string> { "البلد مستخدمة على مستخدم أو أكلة" });

            await repository.DeleteAsync(e => e.Id == request.Id);
            if (await unitOfWork.SaveChangeAsync() <= 0)
                return new Result(HttpStatusCode.InternalServerError, new List<string> { "Error" });

            return new Result(HttpStatusCode.OK, null);
        }
    }
}
