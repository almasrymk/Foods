namespace Foods.Application.MasterData.Meal.Commands.Update
{
    using Foods.Application.Abstraction.Command;
    
    public sealed record UpdateCommand(Guid Id, string Name) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id, string Name);

}