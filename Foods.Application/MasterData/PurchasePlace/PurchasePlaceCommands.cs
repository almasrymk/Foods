namespace Foods.Application.MasterData.PurchasePlace
{
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Query;
    using Foods.Application.Abstraction.Command;

    public sealed record CreatePurchasePlaceCommand(string Name, string? IconPath, string? Color, string? TextColor, int Sort = 0) : ICommand<Guid>;
    public sealed record UpdatePurchasePlaceCommand(Guid Id, string Name, string? IconPath, string? Color, string? TextColor, int Sort = 0) : ICommand<Guid>;
    public sealed record DeletePurchasePlaceCommand(Guid Id) : ICommand<Guid>;
    public sealed record GetPurchasePlacesQuery() : IQuery<List<PurchasePlaceItem>>;
    public sealed record GetPurchasePlaceByIdQuery(Guid Id) : IQuery<PurchasePlaceItem>;
    public sealed record PurchasePlaceItem(Guid Id, string? Name, string? IconPath, string? Color, string? TextColor, int Sort = 0);

    public sealed class CreatePurchasePlaceCommandHandler(IFoodsDbContext db) : ICommandHandler<CreatePurchasePlaceCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreatePurchasePlaceCommand request, CancellationToken cancellationToken)
        {
            var place = new Foods.Domain.Entity.MasterData.PurchasePlace
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                IconPath = request.IconPath,
                Color = request.Color,
                TextColor = request.TextColor,
                Sort = request.Sort,
                IsDeleted = false,
                Status = 0
            };
            db.PurchasePlaces.Add(place);
            await db.SaveChangesAsync(cancellationToken);
            return new Result<Guid>(HttpStatusCode.OK, place.Id, null);
        }
    }

    public sealed class UpdatePurchasePlaceCommandHandler(IFoodsDbContext db) : ICommandHandler<UpdatePurchasePlaceCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(UpdatePurchasePlaceCommand request, CancellationToken cancellationToken)
        {
            var place = await db.PurchasePlaces.FirstOrDefaultAsync(e => e.Id == request.Id && !e.IsDeleted, cancellationToken);
            if (place == null)
                return new Result<Guid>(HttpStatusCode.NotFound, Guid.Empty, new List<string> { "مكان الشراء غير موجود" });
            place.Name = request.Name;
            place.IconPath = request.IconPath;
            place.Color = request.Color;
            place.TextColor = request.TextColor;
            place.Sort = request.Sort;
            await db.SaveChangesAsync(cancellationToken);
            return new Result<Guid>(HttpStatusCode.OK, place.Id, null);
        }
    }

    public sealed class DeletePurchasePlaceCommandHandler(IFoodsDbContext db) : ICommandHandler<DeletePurchasePlaceCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(DeletePurchasePlaceCommand request, CancellationToken cancellationToken)
        {
            var place = await db.PurchasePlaces.FirstOrDefaultAsync(e => e.Id == request.Id && !e.IsDeleted, cancellationToken);
            if (place == null)
                return new Result<Guid>(HttpStatusCode.NotFound, Guid.Empty, new List<string> { "مكان الشراء غير موجود" });
            place.IsDeleted = true;
            await db.SaveChangesAsync(cancellationToken);
            return new Result<Guid>(HttpStatusCode.OK, place.Id, null);
        }
    }

    public sealed class GetPurchasePlacesQueryHandler(IFoodsDbContext db) : IQueryHandler<GetPurchasePlacesQuery, List<PurchasePlaceItem>>
    {
        public async Task<Result<List<PurchasePlaceItem>>> Handle(GetPurchasePlacesQuery request, CancellationToken cancellationToken)
        {
            var places = await db.PurchasePlaces
                .Where(e => !e.IsDeleted)
                .OrderBy(e => e.Sort).ThenBy(e => e.Name)
                .Select(e => new PurchasePlaceItem(e.Id, e.Name, e.IconPath, e.Color, e.TextColor, e.Sort))
                .ToListAsync(cancellationToken);
            return new Result<List<PurchasePlaceItem>>(HttpStatusCode.OK, places, null);
        }
    }

    public sealed class GetPurchasePlaceByIdQueryHandler(IFoodsDbContext db) : IQueryHandler<GetPurchasePlaceByIdQuery, PurchasePlaceItem>
    {
        public async Task<Result<PurchasePlaceItem>> Handle(GetPurchasePlaceByIdQuery request, CancellationToken cancellationToken)
        {
            var place = await db.PurchasePlaces.FirstOrDefaultAsync(e => e.Id == request.Id && !e.IsDeleted, cancellationToken);
            if (place == null)
                return new Result<PurchasePlaceItem>(HttpStatusCode.NotFound, null, new List<string> { "مكان الشراء غير موجود" });
            return new Result<PurchasePlaceItem>(
                HttpStatusCode.OK,
                new PurchasePlaceItem(place.Id, place.Name, place.IconPath, place.Color, place.TextColor, place.Sort),
                null);
        }
    }
}
