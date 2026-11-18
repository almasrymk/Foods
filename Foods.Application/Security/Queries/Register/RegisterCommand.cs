namespace Foods.Application.Security.Queries.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record FamilyMemberInput(string? Name, int Age, DateTime? BirthDate = null, string? Relation = null, string? ImagePath = null);

    public sealed record RegisterCommand(string Name, string UserName, string Password, string? ImagePath, Guid CountryId, IReadOnlyList<FamilyMemberInput>? Family) : ICommand<RegisterCommandResponse>;

    public sealed record RegisterCommandResponse(Guid Id , string Name ,  string UserName , string? ImagePath , string? Token);    
}