namespace Foods.Application.Abstraction.Security
{
    public interface ICurrentUser
    {
        Guid UserId { get; }
    }
}
