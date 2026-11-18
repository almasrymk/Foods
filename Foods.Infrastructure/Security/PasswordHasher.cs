namespace Foods.Infrastructure.Security
{
    using Microsoft.AspNetCore.Identity;

    public sealed class PasswordHasher : Foods.Application.Abstraction.Security.IPasswordHasher
    {
        private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> hasher = new();

        public string Hash(string password)
        {
            return hasher.HashPassword(new object(), password);
        }

        public bool Verify(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                return false;

            try
            {
                var result = hasher.VerifyHashedPassword(new object(), passwordHash, password);
                return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
