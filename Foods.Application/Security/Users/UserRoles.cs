namespace Foods.Application.Security.Users
{
    using Foods.Domain.Entity.Security;

    public static class UserRoles
    {
        public static bool TryParse(string? role, out Guid roleId)
        {
            roleId = Guid.Empty;
            if (string.Equals(role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
            {
                roleId = RoleIds.Admin;
                return true;
            }
            if (string.Equals(role, RoleNames.Customer, StringComparison.OrdinalIgnoreCase))
            {
                roleId = RoleIds.Customer;
                return true;
            }
            return false;
        }

        public static string Name(Guid roleId)
        {
            if (roleId == RoleIds.Admin) return RoleNames.Admin;
            if (roleId == RoleIds.Customer) return RoleNames.Customer;
            return "";
        }
    }
}
