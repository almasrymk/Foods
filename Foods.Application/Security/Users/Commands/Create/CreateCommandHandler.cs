namespace Foods.Application.Security.Users.Commands.Create
{
    using System.Net;
    using Foods.Application.Abstraction.Command;
    using Foods.Application.Abstraction.Security;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using Foods.Domain.Shared;

    public sealed class CreateCommandHandler(
        IUnitOfWork unitOfWork,
        IRepository<User> repository,
        IRepository<Foods.Domain.Entity.MasterData.Country> countries,
        IPasswordHasher passwordHasher) : ICommandHandler<CreateCommand, CreateCommandResponse>
    {
        public async Task<Result<CreateCommandResponse>> Handle(CreateCommand request, CancellationToken cancellationToken)
        {
            if (!UserRoles.TryParse(request.Role, out var roleId))
                return new Result<CreateCommandResponse>(HttpStatusCode.BadRequest, null, new List<string> { "الدور غير معروف" });

            var countryError = await CountryCheck.Validate(countries, roleId, request.CountryId);
            if (countryError != null)
                return new Result<CreateCommandResponse>(HttpStatusCode.BadRequest, null, new List<string> { countryError });

            var userName = request.UserName.Trim();
            var taken = await repository.GetByFilterAsync(e => e.UserName == userName);
            if (taken != null)
                return new Result<CreateCommandResponse>(HttpStatusCode.BadRequest, null, new List<string> { "اسم الدخول موجود" });

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                UserName = userName,
                Password = passwordHasher.Hash(request.Password),
                RoleId = roleId,
                CountryId = request.CountryId,
                IsDeleted = false,
                Status = 0
            };
            await repository.CreateAsync(user);
            if (await unitOfWork.SaveChangeAsync() <= 0)
                return new Result<CreateCommandResponse>(HttpStatusCode.InternalServerError, null, new List<string> { "Error" });

            return new Result<CreateCommandResponse>(HttpStatusCode.OK, new CreateCommandResponse(user.Id), null);
        }
    }
}
