namespace Foods.Application.Abstraction.Jwt
{
    public interface ITokenProvider
    {
        string CreateToken(Guid Id, string UserName, string Role);
    }
}