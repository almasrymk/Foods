namespace Foods.Application.Security.Queries.Create
{
    using System.Net;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using Microsoft.Extensions.Logging;
    using Foods.Application.Abstraction.Jwt;
    using Foods.Application.Abstraction.Command;
    using Foods.Application.Abstraction.Security;
    using Foods.Application.Security;

    public sealed class LoginCommandHandler(
        IRepository<User> repository,
        ITokenProvider tokenProvider,
        IPasswordHasher passwordHasher,
        IFoodsDbContext db,
        Foods.Application.Customer.Notifications.NotificationGenerator notifications,
        ILogger<LoginCommandHandler> logger) : ICommandHandler<LoginCommand, LoginCommandResponse>
    {
        public async Task<Result<LoginCommandResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Customer login");
            var user = await repository.GetByFilterAsync(e => e.UserName == request.UserName && !e.IsDeleted);
            var isCustomer = user?.RoleId == RoleIds.Customer;
            var isAdmin = user?.RoleId == RoleIds.Admin;
            if (user == null || (!isCustomer && !isAdmin) || !await PasswordCheck.MatchesAsync(user, request.Password, passwordHasher, repository))
            {
                return new Result<LoginCommandResponse>(
                    HttpStatusCode.Unauthorized,
                    null,
                    new List<string> { "بيانات الدخول غير صحيحة" });
            }

            // Track the login; a customer back after a long absence gets a welcome-back notification.
            var now = DateTime.UtcNow;
            if (isCustomer)
            {
                try { await notifications.WelcomeBackAsync(user.Id, user.Name, user.LastLoginAt, now, cancellationToken); }
                catch (Exception ex) { logger.LogWarning(ex, "Welcome-back notification failed"); }
            }
            user.LastLoginAt = now;
            await db.SaveChangesAsync(cancellationToken);

            var role = isAdmin ? RoleNames.Admin : RoleNames.Customer;
            return new Result<LoginCommandResponse>(HttpStatusCode.OK, new LoginCommandResponse
            (
               user.Id,
               user.Name!,
               user.UserName!,
               user.ImagePath,
               tokenProvider.CreateToken(user.Id, user.UserName!, role),
               role
            ), null);
        }
    }
}
