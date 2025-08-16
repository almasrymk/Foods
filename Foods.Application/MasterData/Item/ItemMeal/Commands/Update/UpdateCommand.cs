namespace Foods.Application.MasterData.Item.ItemMeal.Commands.Update
{
    using Foods.Application.Abstraction.Command;
    
    public sealed record UpdateCommand(Guid Id, Guid ItemId, Guid MealId) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id);

}