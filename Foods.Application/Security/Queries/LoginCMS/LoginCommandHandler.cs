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

    public sealed class LoginCMSCommandHandler(IRepository<User> _Repository, IJwtTokenGenerator _JwtTokenGenerator) : ICommandHandler<LoginCMSCommand, LoginCMSCommandResponse>
    {
        public async Task<Result<LoginCMSCommandResponse>> Handle(LoginCMSCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _Repository.GetByFilterAsync(e => e.UserName == request.UserName);
                if (user != null)
                {
                    return new Result<LoginCMSCommandResponse>(HttpStatusCode.OK, new LoginCMSCommandResponse
                    (
                       user.Id,
                       user.Name!,
                       user.UserName!,
                       user.RoleId,
                       user.ImagePath,
                       _JwtTokenGenerator.GenerateToken(user.UserName!)
                    ), null);
                }

                return new Result<LoginCMSCommandResponse>(
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