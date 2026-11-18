namespace Foods.Application.MasterData.Cuisine.Commands.Delete
{
    using Foods.Application.Abstraction.Command;

    public sealed record DeleteCommand(Guid Id) : ICommand;
}
