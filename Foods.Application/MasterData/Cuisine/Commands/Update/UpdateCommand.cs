namespace Foods.Application.MasterData.Cuisine.Commands.Update
{
    using Foods.Application.Abstraction.Command;

    public sealed record UpdateCommand(Guid Id, string Name, string? IconPath, string? Color, string? TextColor, int Sort, string? ImagePath = null, Guid? CountryId = null) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id);
}
