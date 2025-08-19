namespace Foods.Application.Security.Queries.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record LoginCMSCommand(string UserName , string Password ) : ICommand<LoginCMSCommandResponse>;

    public sealed record LoginCMSCommandResponse(Guid Id , string Name ,  string UserName , Guid RoleId , string? ImagePath , string? Token);    
}