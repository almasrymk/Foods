namespace Foods.Application.Security.Queries.Create
{
    using System.Net;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using Foods.Application.Abstraction.Jwt;
    using Foods.Application.Abstraction.Command;
    using Foods.Application.Abstraction.Security;
    using Foods.Application.Security;

    public sealed class LoginCMSCommandHandler(
        IRepository<User> repository,
        ITokenProvider tokenProvider,
        IPasswordHasher passwordHasher) : ICommandHandler<LoginCMSCommand, LoginCMSCommandResponse>
    {
        public async Task<Result<LoginCMSCommandResponse>> Handle(LoginCMSCommand request, CancellationToken cancellationToken)
        {
            var user = await repository.GetByFilterAsync(e => e.UserName == request.UserName && !e.IsDeleted);
            if (user == null || user.RoleId != RoleIds.Admin || !await PasswordCheck.MatchesAsync(user, request.Password, passwordHasher, repository))
            {
                return new Result<LoginCMSCommandResponse>(
                    HttpStatusCode.Unauthorized,
                    null,
                    new List<string> { "بيانات الدخول غير صحيحة" });
            }

            return new Result<LoginCMSCommandResponse>(HttpStatusCode.OK, new LoginCMSCommandResponse
            (
               user.Id,
               user.Name!,
               user.UserName!,
               user.RoleId,
               user.ImagePath,
               tokenProvider.CreateToken(user.Id, user.UserName!, RoleNames.Admin)
            ), null);
        }
    }
}
