namespace Foods.Application.MasterData.Content.ContentLevel.Commands.Update
{
    using Foods.Application.Abstraction.Command;
    
    public sealed record UpdateCommand(Guid Id, Guid ContentId , Guid LevelId) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id, Guid ContentId, Guid LevelId);

}