namespace Foods.Application.Abstraction.Jwt
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(string username);
    }
}