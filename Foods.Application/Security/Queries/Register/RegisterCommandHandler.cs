namespace Foods.Application.Security.Queries.Create
{
    using System.Net;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using Foods.Application.Abstraction.Jwt;
    using Foods.Application.Abstraction.Command;
    using Foods.Application.Abstraction.Security;

    public sealed class RegisterCommandHandler(
        IRepository<User> repository,
        IRepository<FamilyMember> familyRepository,
        IRepository<Foods.Domain.Entity.MasterData.Country> countries,
        ITokenProvider tokenProvider,
        IPasswordHasher passwordHasher) : ICommandHandler<RegisterCommand, RegisterCommandResponse>
    {
        public async Task<Result<RegisterCommandResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var existing = await repository.GetByFilterAsync(e => e.UserName == request.UserName);
            if (existing != null)
            {
                return new Result<RegisterCommandResponse>(
                    HttpStatusCode.BadRequest,
                    null,
                    new List<string> { "اسم المستخدم مستخدم" });
            }

            // Family members are optional here: the app asks for them on the next screen (it can be skipped).
            var country = await countries.GetByFilterAsync(e => e.Id == request.CountryId);
            if (country == null)
            {
                return new Result<RegisterCommandResponse>(
                    HttpStatusCode.BadRequest,
                    null,
                    new List<string> { "البلد غير موجودة" });
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Password = passwordHasher.Hash(request.Password),
                UserName = request.UserName,
                ImagePath = request.ImagePath,
                RoleId = RoleIds.Customer,
                CountryId = request.CountryId,
                LastLoginAt = DateTime.UtcNow,
                IsDeleted = false,
                Status = 0
            };

            await repository.CreateAsync(user);
            foreach (var member in request.Family ?? new List<FamilyMemberInput>())
            {
                await familyRepository.CreateAsync(new FamilyMember
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Name = member.Name,
                    Age = member.Age,
                    BirthDate = member.BirthDate?.Date,
                    IsDeleted = false,
                    Status = 0
                });
            }

            if (!await repository.Commit())
            {
                return new Result<RegisterCommandResponse>(
                    HttpStatusCode.InternalServerError,
                    null,
                    new List<string> { "تعذر إنشاء الحساب" });
            }

            return new Result<RegisterCommandResponse>(HttpStatusCode.OK, new RegisterCommandResponse
            (
               user.Id,
               user.Name!,
               user.UserName!,
               user.ImagePath,
               tokenProvider.CreateToken(user.Id, user.UserName!, RoleNames.Customer)
            ), null);
        }
    }
}
