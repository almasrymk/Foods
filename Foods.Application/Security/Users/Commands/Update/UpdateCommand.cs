namespace Foods.Application.Security.Users.Commands.Update
{
    using Foods.Application.Abstraction.Command;

    public sealed record UpdateCommand(Guid Id, string Name, string UserName, string? Password, string Role, Guid? CountryId) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id);
}
