namespace Foods.Application.MasterData.Content.ContentAlternative.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(Guid ContentId , Guid ContentAlternativeId) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);

}