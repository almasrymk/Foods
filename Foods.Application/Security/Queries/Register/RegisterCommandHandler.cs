namespace Foods.Application.Security.Queries.Create
{
    using System.Net;
    using System.Threading;
    using Foods.Domain.Shared;
    using System.Threading.Tasks;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using Foods.Application.Abstraction.Jwt;
    using Foods.Application.Abstraction.Command;

    public sealed class RegisterCommandHandler(IRepository<User> _Repository, ITokenProvider tokenProvider) : ICommandHandler<RegisterCommand, RegisterCommandResponse>
    {
        public async Task<Result<RegisterCommandResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Password = request.Password,
                    UserName = request.UserName,
                    ImagePath = request.ImagePath,
                    RoleId = Guid.Parse("42997ac6-a9c6-4e97-f108-3a1a18b2ba01"),
                    IsDeleted = false,
                    Status = 0
                };

                await _Repository.CreateAsync(user);
                var res =  await _Repository.Commit();
                if (res)
                {
                    return new Result<RegisterCommandResponse>(HttpStatusCode.OK, new RegisterCommandResponse
                    (
                       user.Id,
                       user.Name!,
                       user.UserName!,
                       user.ImagePath,
                       tokenProvider.CreateToken(user.Id, user.UserName!)
                    ), null);
                }

                return new Result<RegisterCommandResponse>(
                HttpStatusCode.InternalServerError,
                null,
                new List<string> { "Error" });
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}