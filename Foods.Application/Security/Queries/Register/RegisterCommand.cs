namespace Foods.Application.Security.Queries.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record RegisterCommand(string Name  , string UserName , string Password, string? ImagePath) : ICommand<RegisterCommandResponse>;

    public sealed record RegisterCommandResponse(Guid Id , string Name ,  string UserName , string? ImagePath , string? Token);    
}