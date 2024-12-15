namespace Foods.Application.MasterData.Content.ContentCategory.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(string Name) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id, string Name);

}