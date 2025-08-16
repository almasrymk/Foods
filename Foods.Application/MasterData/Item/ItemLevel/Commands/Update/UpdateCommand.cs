namespace Foods.Application.MasterData.Item.ItemLevel.Commands.Update
{
    using Foods.Application.Abstraction.Command;
    
    public sealed record UpdateCommand(Guid Id, Guid ItemId, Guid LevelId) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id);

}