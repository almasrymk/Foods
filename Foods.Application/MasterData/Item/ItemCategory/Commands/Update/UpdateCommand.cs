namespace Foods.Application.MasterData.Item.ItemCategory.Commands.Update
{
    using Foods.Application.Abstraction.Command;
    
    public sealed record UpdateCommand(Guid Id, string Name, string? IconPath, string? Color, string? TextColor, int Sort = 0) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id);

}