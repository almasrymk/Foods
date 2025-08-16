namespace Foods.Application.MasterData.Unit.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(string Name, string? NickName , decimal BasicQuantity , bool BeDivided) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);

}