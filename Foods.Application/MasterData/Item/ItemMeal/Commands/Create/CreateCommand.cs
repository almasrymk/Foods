namespace Foods.Application.MasterData.Item.ItemMeal.Commands.Create
{
    using Foods.Application.Abstraction.Command;

    public sealed record CreateCommand(Guid ItemId , Guid MealId) : ICommand<CreateCommandResponse>;

    public sealed record CreateCommandResponse(Guid Id);

}