namespace Foods.Application.MasterData.Meal.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(string Name, string Description, string? ImagePath, string? IconPath, string? Color, string? TextColor, int Sort) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);

}
