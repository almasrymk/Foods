namespace Foods.Infrastructure.Persistence.Jwt
{
    using System.Text;
    using System.Security.Claims; 
    using Microsoft.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt; 
    using Foods.Application.Abstraction.Jwt;

    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        public string GenerateToken(string username)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsMyUltraSecureKey1234567890!@#$")); // >= 32 chars
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] { new Claim(ClaimTypes.Name, username), new Claim(ClaimTypes.Role, "Admin") };

            var token = new JwtSecurityToken(
                issuer: "Foods",
                audience: "Foods",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}