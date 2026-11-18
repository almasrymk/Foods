namespace Foods.Application.MasterData.Country.Commands.Delete
{
    using Foods.Application.Abstraction.Command;

    public sealed record DeleteCommand(Guid Id) : ICommand;
}
