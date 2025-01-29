namespace Foods.Application.MasterData.Content.ContentLevel.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(Guid ContentId , Guid LevelId) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id, Guid ContentId, Guid LevelId);

}