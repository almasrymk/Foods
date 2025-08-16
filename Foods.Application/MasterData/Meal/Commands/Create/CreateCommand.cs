namespace Foods.Application.MasterData.Meal.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(string Name , string Description) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);

}