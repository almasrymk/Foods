namespace Foods.Application.MasterData.Item.ItemCountry.Commands.Update
{
    using Foods.Application.Abstraction.Command;

    public sealed record UpdateCommand(Guid Id, Guid ItemId, Guid CountryId, bool IsPrimary, string? Name, string? Summary, string? Description) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id);
}
