namespace Foods.Application.MasterData.Item.ItemGroup.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(string Name) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);

}