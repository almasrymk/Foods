namespace Foods.Infrastructure.Security
{
    using System.Security.Claims;
    using Microsoft.AspNetCore.Http;
    using Foods.Application.Abstraction.Security;

    public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        public Guid UserId
        {
            get
            {
                var value = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(value, out var id))
                    return id;

                throw new UnauthorizedAccessException("المستخدم غير مسجل.");
            }
        }
    }
}
