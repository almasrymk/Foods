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

    public sealed class LoginCommandHandler(IRepository<User> _Repository , ITokenProvider tokenProvider) : ICommandHandler<LoginCommand, LoginCommandResponse>
    {
        public async Task<Result<LoginCommandResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _Repository.GetByFilterAsync(e => e.UserName == request.UserName);
                if (user != null)
                {
                    return new Result<LoginCommandResponse>(HttpStatusCode.OK, new LoginCommandResponse
                    (
                       user.Id,
                       user.Name!,
                       user.UserName!,
                       user.ImagePath,
                       //_JwtTokenGenerator.GenerateToken(user.UserName!)
                       tokenProvider.CreateToken(user.Id, user.Name!)
                    ), null);
                }

                return new Result<LoginCommandResponse>(
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