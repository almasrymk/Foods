namespace Foods.Application.Security.Users.Commands.Delete
{
    using System.Net;
    using Foods.Application.Abstraction.Command;
    using Foods.Application.Abstraction.Security;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using Foods.Domain.Shared;

    public sealed class DeleteCommandHandler(
        IUnitOfWork unitOfWork,
        IRepository<User> repository,
        ICurrentUser currentUser) : ICommandHandler<DeleteCommand>
    {
        public async Task<Result> Handle(DeleteCommand request, CancellationToken cancellationToken)
        {
            if (request.Id == currentUser.UserId)
                return new Result(HttpStatusCode.BadRequest, new List<string> { "لا يمكن حذف حسابك" });

            var user = await repository.GetByFilterAsync(e => e.Id == request.Id);
            if (user == null)
                return new Result(HttpStatusCode.BadRequest, new List<string> { "المستخدم غير موجود" });

            if (user.RoleId == RoleIds.Admin)
            {
                var others = await repository.GetListByFilterAsync(e => e.RoleId == RoleIds.Admin && e.Id != user.Id);
                if (others == null || !others.Any())
                    return new Result(HttpStatusCode.BadRequest, new List<string> { "لازم يفضل أدمن واحد على الأقل" });
            }

            await repository.DeleteAsync(e => e.Id == request.Id);
            if (await unitOfWork.SaveChangeAsync() <= 0)
                return new Result(HttpStatusCode.InternalServerError, new List<string> { "Error" });

            return new Result(HttpStatusCode.OK, null);
        }
    }
}
