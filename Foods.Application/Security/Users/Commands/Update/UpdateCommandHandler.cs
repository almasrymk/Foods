namespace Foods.Application.Security.Users.Commands.Update
{
    using System.Net;
    using Foods.Application.Abstraction.Command;
    using Foods.Application.Abstraction.Security;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using Foods.Domain.Shared;

    public sealed class UpdateCommandHandler(
        IUnitOfWork unitOfWork,
        IRepository<User> repository,
        IPasswordHasher passwordHasher,
        IRepository<Foods.Domain.Entity.MasterData.Country> countries,
        ICurrentUser currentUser) : ICommandHandler<UpdateCommand, UpdateCommandResponse>
    {
        public async Task<Result<UpdateCommandResponse>> Handle(UpdateCommand request, CancellationToken cancellationToken)
        {
            if (!UserRoles.TryParse(request.Role, out var roleId))
                return new Result<UpdateCommandResponse>(HttpStatusCode.BadRequest, null, new List<string> { "الدور غير معروف" });

            var countryError = await CountryCheck.Validate(countries, roleId, request.CountryId);
            if (countryError != null)
                return new Result<UpdateCommandResponse>(HttpStatusCode.BadRequest, null, new List<string> { countryError });

            var user = await repository.GetByFilterAsync(e => e.Id == request.Id);
            if (user == null)
                return new Result<UpdateCommandResponse>(HttpStatusCode.BadRequest, null, new List<string> { "المستخدم غير موجود" });

            if (user.RoleId == RoleIds.Admin && roleId != RoleIds.Admin && await LastAdmin(repository, user.Id))
                return new Result<UpdateCommandResponse>(HttpStatusCode.BadRequest, null, new List<string> { "لازم يفضل أدمن واحد على الأقل" });

            if (user.Id == currentUser.UserId && roleId != RoleIds.Admin)
                return new Result<UpdateCommandResponse>(HttpStatusCode.BadRequest, null, new List<string> { "لا يمكن تغيير دور حسابك" });

            var userName = request.UserName.Trim();
            var taken = await repository.GetByFilterAsync(e => e.UserName == userName && e.Id != request.Id);
            if (taken != null)
                return new Result<UpdateCommandResponse>(HttpStatusCode.BadRequest, null, new List<string> { "اسم الدخول موجود" });

            user.Name = request.Name.Trim();
            user.UserName = userName;
            user.RoleId = roleId;
            user.CountryId = request.CountryId;
            if (!string.IsNullOrWhiteSpace(request.Password))
                user.Password = passwordHasher.Hash(request.Password);

            await repository.UpdateAsync(user);
            if (await unitOfWork.SaveChangeAsync() <= 0)
                return new Result<UpdateCommandResponse>(HttpStatusCode.InternalServerError, null, new List<string> { "Error" });

            return new Result<UpdateCommandResponse>(HttpStatusCode.OK, new UpdateCommandResponse(user.Id), null);
        }

        private static async Task<bool> LastAdmin(IRepository<User> repository, Guid userId)
        {
            var users = await repository.GetListByFilterAsync(e => e.RoleId == RoleIds.Admin && e.Id != userId);
            return users == null || !users.Any();
        }
    }
}
