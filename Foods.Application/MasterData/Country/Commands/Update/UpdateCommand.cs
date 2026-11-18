namespace Foods.Application.MasterData.Country.Commands.Update
{
    using Foods.Application.Abstraction.Command;

    public sealed record UpdateCommand(Guid Id, string Name, string? Dialect, string? IconPath, string? Color, string? TextColor, string? Currency = null, string? Neighbors = null) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id);
}
