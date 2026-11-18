namespace Foods.Application.MasterData.Item.ItemCountry.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(Guid ItemId, Guid CountryId, bool IsPrimary, string? Name, string? Summary, string? Description) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);
}
