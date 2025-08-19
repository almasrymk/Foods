namespace Foods.Application.Security.Queries.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record LoginCommand(string UserName , string Password ) : ICommand<LoginCommandResponse>;

    public sealed record LoginCommandResponse(Guid Id , string Name ,  string UserName , string? ImagePath , string? Token);    
}