namespace Foods.Application.Security.Users.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(string Name, string UserName, string Password, string Role, Guid? CountryId) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);
}
