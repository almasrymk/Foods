namespace Foods.Application.Security
{
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using Foods.Application.Abstraction.Security;

    public static class PasswordCheck
    {
        public static async Task<bool> MatchesAsync(User user, string password, IPasswordHasher hasher, IRepository<User> repository)
        {
            if (hasher.Verify(password, user.Password ?? ""))
                return true;

            if (!string.IsNullOrEmpty(user.Password) && user.Password == password)
            {
                user.Password = hasher.Hash(password);
                await repository.Commit();
                return true;
            }

            return false;
        }
    }
}
