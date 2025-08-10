namespace Foods.Application.MasterData.Content.ContentPrice.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(Guid ContentId , decimal Price, bool IsCurrent) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);

}