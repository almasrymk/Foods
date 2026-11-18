namespace Foods.Application.MasterData.Cuisine.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(string Name, string? IconPath, string? Color, string? TextColor, int Sort, string? ImagePath = null, Guid? CountryId = null) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);
}
