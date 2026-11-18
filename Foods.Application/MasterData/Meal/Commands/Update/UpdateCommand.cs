namespace Foods.Application.MasterData.Meal.Commands.Update
{
    using Foods.Application.Abstraction.Command;
    
    public sealed record UpdateCommand(Guid Id, string Name, string Description, string? ImagePath, string? IconPath, string? Color, string? TextColor, int Sort) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id);

}
