namespace Foods.Application.MasterData.Country.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(string Name, string? Dialect, string? IconPath, string? Color, string? TextColor, string? Currency = null, string? Neighbors = null) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);
}
