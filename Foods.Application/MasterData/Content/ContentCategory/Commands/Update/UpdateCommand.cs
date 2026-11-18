namespace Foods.Application.MasterData.Content.ContentCategory.Commands.Update
{
    using Foods.Application.Abstraction.Command;
    
    public sealed record UpdateCommand(Guid Id, string Name, string? IconPath, string? Color, string? TextColor) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id, string Name);

}