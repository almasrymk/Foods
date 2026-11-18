namespace Foods.Application.Customer.Favorites
{
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using Foods.Application.Abstraction.Query;
    using Foods.Application.Abstraction.Command;
    using Foods.Application.Abstraction.Security;
    using Foods.Application.Customer.Catalog;

    public sealed record GetFavoritesQuery(string? Text = null, Guid? CuisineId = null, int Skip = 0, int? Take = null, Guid? CategoryId = null, string? Ingredient = null, Guid? GroupId = null, Guid? KitchenId = null, Guid? FilterMealId = null, List<Guid>? CategoryIds = null) : IQuery<List<ItemCard>>;

    public sealed class GetFavoritesQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<GetFavoritesQuery, List<ItemCard>>
    {
        public async Task<Result<List<ItemCard>>> Handle(GetFavoritesQuery request, CancellationToken cancellationToken)
        {
            var text = (request.Text ?? "").Trim().ToLower();
            var skip = Math.Max(0, request.Skip);
            int? take = request.Take is int value && value > 0 ? Math.Clamp(value, 1, 50) : null;

            var view = await MenuDialect.For(db, currentUser.UserId, cancellationToken);
            var favorites = await (
                from favorite in db.Set<Favorite>()
                join item in db.Items on favorite.ItemId equals item.Id
                where favorite.UserId == currentUser.UserId && !favorite.IsDeleted && !item.IsDeleted
                select item).ToListAsync(cancellationToken);

            var spoken = view.CountryId is Guid countryId
                ? await MenuDialect.ForItems(db, countryId, favorites.Select(e => e.Id), cancellationToken)
                : new Dictionary<Guid, Foods.Domain.Entity.MasterData.Item.ItemCountry>();
            if (!view.ShowAll)
            {
                var offered = await MenuDialect.Offered(db, view, favorites.Select(e => e.Id), cancellationToken);
                favorites = favorites.Where(e => offered.Contains(e.Id)).ToList();
            }

            if (request.CuisineId is Guid cuisineFilter)
                favorites = favorites.Where(e => e.CuisineId == cuisineFilter).ToList();

            favorites = await ItemSearch.Filter(db, favorites, view, new ItemSearchOptions(request.Text, request.CategoryId, request.Ingredient, request.KitchenId, request.GroupId, request.FilterMealId, request.CategoryIds), cancellationToken);
            text = "";

            var categoryIds = favorites.Select(e => e.ItemCategoryId).Distinct().ToList();
            var categories = await db.ItemCategories
                .Where(e => categoryIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);
            var groupIds = favorites.Select(e => e.ItemGroupId).Distinct().ToList();
            var groups = await db.ItemGroups
                .Where(e => groupIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);
            var cuisineIds = favorites.Where(e => e.CuisineId != null).Select(e => e.CuisineId!.Value).Distinct().ToList();
            var cuisines = await db.Cuisines
                .Where(e => cuisineIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);

            var book = await Foods.Application.Customer.Pricing.PriceBook.For(db, currentUser.UserId, cancellationToken);
            var prices = book == null ? new() : await book.Dishes(favorites.Select(e => e.Id).ToList(), cancellationToken);
            var cards = favorites
                .Where(item =>
                {
                    spoken.TryGetValue(item.Id, out var local);
                    var name = MenuDialect.Pick(local?.Name, item.Name) ?? "";
                    var summary = MenuDialect.Pick(local?.Summary, item.Summary) ?? "";
                    return text.Length == 0 || name.ToLower().Contains(text) || summary.ToLower().Contains(text);
                })
                .OrderBy(e => MenuDialect.Pick(spoken.GetValueOrDefault(e.Id)?.Name, e.Name))
                .Skip(skip)
                .Take(take ?? int.MaxValue)
                .Select(item =>
                {
                    spoken.TryGetValue(item.Id, out var local);
                    categories.TryGetValue(item.ItemCategoryId, out var categoryName);
                    string? cuisineName = null;
                    if (item.CuisineId is Guid cuisineId)
                        cuisines.TryGetValue(cuisineId, out cuisineName);
                    return new ItemCard(
                        item.Id,
                        MenuDialect.Pick(local?.Name, item.Name),
                        MenuDialect.Pick(local?.Summary, item.Summary),
                        item.ImagePath,
                        true,
                        view.CountryName,
                        view.Dialect,
                        item.ItemCategoryId,
                        categoryName,
                        item.CuisineId,
                        cuisineName,
                        null,
                        item.ItemGroupId,
                        groups.GetValueOrDefault(item.ItemGroupId),
                        book == null ? null : prices.GetValueOrDefault(item.Id)?.Total ?? 0,
                        prices.GetValueOrDefault(item.Id)?.Missing ?? 0);
                })
                .ToList();

            return new Result<List<ItemCard>>(HttpStatusCode.OK, cards, null);
        }
    }

    public sealed record AddFavoriteCommand(Guid ItemId) : ICommand<bool>;

    public sealed class AddFavoriteCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<AddFavoriteCommand, bool>
    {
        public async Task<Result<bool>> Handle(AddFavoriteCommand request, CancellationToken cancellationToken)
        {
            var itemExists = await db.Items.AnyAsync(e => e.Id == request.ItemId && !e.IsDeleted, cancellationToken);
            if (!itemExists)
                return new Result<bool>(HttpStatusCode.NotFound, false, new List<string> { "الأكلة غير موجودة" });

            var favorite = await db.Set<Favorite>().FirstOrDefaultAsync(
                e => e.UserId == currentUser.UserId && e.ItemId == request.ItemId, cancellationToken);

            if (favorite == null)
            {
                db.Set<Favorite>().Add(new Favorite
                {
                    Id = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    ItemId = request.ItemId,
                    IsDeleted = false,
                    Status = 0
                });
            }
            else
            {
                favorite.IsDeleted = false;
            }

            await db.SaveChangesAsync(cancellationToken);
            return new Result<bool>(HttpStatusCode.OK, true, null);
        }
    }

    public sealed record RemoveFavoriteCommand(Guid ItemId) : ICommand<bool>;

    public sealed class RemoveFavoriteCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<RemoveFavoriteCommand, bool>
    {
        public async Task<Result<bool>> Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken)
        {
            var favorite = await db.Set<Favorite>().FirstOrDefaultAsync(
                e => e.UserId == currentUser.UserId && e.ItemId == request.ItemId && !e.IsDeleted, cancellationToken);
            if (favorite != null)
            {
                favorite.IsDeleted = true;
                await db.SaveChangesAsync(cancellationToken);
            }

            return new Result<bool>(HttpStatusCode.OK, true, null);
        }
    }
}
