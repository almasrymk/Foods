namespace Foods.Application.MasterData.Item.ItemGroup.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(string Name, string? IconPath, string? Color, string? TextColor, int Sort = 0) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);

}