namespace Foods.Application.MasterData.Content.Content.Commands.Update
{
    using Foods.Application.Abstraction.Command;
    
    public sealed record UpdateCommand(Guid Id, string Name, Guid ContentCategoryId, Guid ContentGroupId, Guid UnitId, decimal MiniQuantity, string? ImagePath, Guid? PurchasePlaceId, int PurchaseMode = 0, string? PackageName = null, decimal PurchaseStep = 0, decimal PurchaseIncrement = 0, decimal PurchaseUnitPrice = 0, decimal PurchaseMinAmount = 0, decimal Price = 0, decimal PriceQuantity = 1, Guid? PriceUnitId = null, Guid? CuisineId = null, int Sort = 0) : ICommand<UpdateCommandResponse>;

    public sealed record UpdateCommandResponse(Guid Id);

}