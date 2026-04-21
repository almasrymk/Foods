namespace Foods.Infrastructure.Persistence.Jwt
{
    using System.Text;
    using System.Security.Claims;
    using Microsoft.Extensions.Logging;
    using Microsoft.IdentityModel.Tokens;
    using Foods.Application.Abstraction.Jwt;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.JsonWebTokens;

    public sealed class TokenProvider(IConfiguration configuration , ILogger<TokenProvider> logger) : ITokenProvider
    {
        public string CreateToken(Guid Id , string UserName)
        {
            logger.LogInformation("Start Create Token");
            string secretKey = configuration["Jwt:Key"]!;
            var securitytKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securitytKey, SecurityAlgorithms.HmacSha256);
            var TokenDescriptore = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub , "" + Id),
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email , UserName)
                ]),
                Expires = DateTime.UtcNow.AddDays(30),
                SigningCredentials = credentials,
                Issuer = configuration["Jwt:Issuer"],
                Audience = configuration["Jwt:Audience"],
            };
            logger.LogInformation("Finsh Create Token");
            return new JsonWebTokenHandler().CreateToken(TokenDescriptore);
        }
    }
}