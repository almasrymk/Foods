namespace Foods.Application.Customer.Catalog
{
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Security;
    using Foods.Application.Abstraction.Query;
    using Foods.Application.Abstraction.Security;

    public sealed record GetMealsQuery() : IQuery<List<MealCard>>;
    /// <param name="Count">How many dishes the customer can see in this meal (shown on the tile like the cuisines).</param>
    public sealed record MealCard(Guid Id, string? Name, string? Description, string? ImagePath, string? IconPath, string? Color, string? TextColor, int Sort, int Count = 0);

    public sealed class GetMealsQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<GetMealsQuery, List<MealCard>>
    {
        public async Task<Result<List<MealCard>>> Handle(GetMealsQuery request, CancellationToken cancellationToken)
        {
            var meals = await db.Meals
                .Where(e => !e.IsDeleted)
                .OrderBy(e => e.Sort)
                .ThenBy(e => e.Name)
                .Select(e => new MealCard(e.Id, e.Name, e.Description, e.ImagePath, e.IconPath, e.Color, e.TextColor, e.Sort, 0))
                .ToListAsync(cancellationToken);

            var view = await MenuDialect.For(db, currentUser.UserId, cancellationToken);
            for (var i = 0; i < meals.Count; i++)
                meals[i] = meals[i] with { Count = await MealMenu.QueryFor(db, meals[i].Id, view).CountAsync(cancellationToken) };

            return new Result<List<MealCard>>(HttpStatusCode.OK, meals, null);
        }
    }

    public sealed record GetItemsQuery(Guid? MealId, string? Text, Guid? CuisineId, int Skip = 0, int? Take = null, Guid? CategoryId = null, string? Ingredient = null, Guid? GroupId = null, Guid? KitchenId = null, Guid? FilterMealId = null, List<Guid>? CategoryIds = null) : IQuery<List<ItemCard>>;

    public sealed record ItemCard(Guid Id, string? Name, string? Summary, string? ImagePath, bool IsFavorite, string? CountryName, string? Dialect, Guid? CategoryId, string? CategoryName, Guid? CuisineId, string? CuisineName, List<string>? Images = null, Guid? GroupId = null, string? GroupName = null, decimal? Price = null, int PriceMissing = 0, Guid? RecipeCountryId = null);

    public sealed record GetMealItemFiltersQuery(Guid? MealId) : IQuery<List<MealItemFilter>>;
    public sealed record GetCuisinesQuery() : IQuery<List<MealItemFilter>>;
    public sealed record GetSuggestedItemsQuery(string? Text, Guid? CuisineId, int Skip = 0, int Take = 8, Guid? CategoryId = null, string? Ingredient = null, Guid? GroupId = null, Guid? KitchenId = null, Guid? FilterMealId = null, List<Guid>? CategoryIds = null) : IQuery<List<ItemCard>>;
    public sealed record GetFavoriteFiltersQuery() : IQuery<List<MealItemFilter>>;
    /// <param name="IsMine">A cuisine of the customer's own country (listed first; the meal screen starts on it).</param>
    public sealed record MealItemFilter(Guid? Id, string Name, int Count, string? IconPath = null, string? Color = null, string? TextColor = null, string? ImagePath = null, bool IsMine = false);

    public sealed class GetMealItemFiltersQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<GetMealItemFiltersQuery, List<MealItemFilter>>
    {
        public async Task<Result<List<MealItemFilter>>> Handle(GetMealItemFiltersQuery request, CancellationToken cancellationToken)
        {
            if (request.MealId is null)
            {
                var items = await MealMenu.VisibleItems(db, currentUser, cancellationToken);
                var mine = await MenuDialect.For(db, currentUser.UserId, cancellationToken);
                return new Result<List<MealItemFilter>>(HttpStatusCode.OK, await CuisineFilters.Build(db, items, cancellationToken, mine), null);
            }

            var view = await MenuDialect.For(db, currentUser.UserId, cancellationToken);
            var query = MealMenu.QueryFor(db, request.MealId.Value, view);
            var total = await query.CountAsync(cancellationToken);
            var counts = await query
                .Where(e => e.CuisineId != null)
                .GroupBy(e => e.CuisineId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var countMap = counts.ToDictionary(e => e.Id, e => e.Count);

            // Cuisines of the customer's own country first.
            var cuisines = CuisineOrder.Sort(await db.Cuisines
                .Where(e => !e.IsDeleted)
                .ToListAsync(cancellationToken), view);

            var filters = new List<MealItemFilter> { new(null, "الكل", total, null, null, null) };
            filters.AddRange(cuisines.Select(cuisine =>
                new MealItemFilter(
                    cuisine.Id,
                    cuisine.Name ?? "",
                    countMap.GetValueOrDefault(cuisine.Id),
                    cuisine.IconPath,
                    cuisine.Color,
                    cuisine.TextColor,
                    cuisine.ImagePath,
                    CuisineOrder.IsMine(cuisine, view))));

            return new Result<List<MealItemFilter>>(HttpStatusCode.OK, filters, null);
        }
    }

    public sealed class GetCuisinesQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<GetCuisinesQuery, List<MealItemFilter>>
    {
        public async Task<Result<List<MealItemFilter>>> Handle(GetCuisinesQuery request, CancellationToken cancellationToken)
        {
            var items = await MealMenu.VisibleItems(db, currentUser, cancellationToken);
            var view = await MenuDialect.For(db, currentUser.UserId, cancellationToken);
            return new Result<List<MealItemFilter>>(HttpStatusCode.OK, await CuisineFilters.Build(db, items, cancellationToken, view), null);
        }
    }

    public sealed class GetFavoriteFiltersQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<GetFavoriteFiltersQuery, List<MealItemFilter>>
    {
        public async Task<Result<List<MealItemFilter>>> Handle(GetFavoriteFiltersQuery request, CancellationToken cancellationToken)
        {
            var items = await (
                from favorite in db.Set<Favorite>()
                join item in db.Items on favorite.ItemId equals item.Id
                where favorite.UserId == currentUser.UserId && !favorite.IsDeleted && !item.IsDeleted
                select item).ToListAsync(cancellationToken);
            var view = await MenuDialect.For(db, currentUser.UserId, cancellationToken);
            if (!view.ShowAll)
            {
                var offered = await MenuDialect.Offered(db, view, items.Select(e => e.Id), cancellationToken);
                items = items.Where(e => offered.Contains(e.Id)).ToList();
            }
            return new Result<List<MealItemFilter>>(HttpStatusCode.OK, await CuisineFilters.Build(db, items, cancellationToken, view), null);
        }
    }

    public sealed class GetSuggestedItemsQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<GetSuggestedItemsQuery, List<ItemCard>>
    {
        const int MaxSuggested = 20;

        public async Task<Result<List<ItemCard>>> Handle(GetSuggestedItemsQuery request, CancellationToken cancellationToken)
        {
            var text = (request.Text ?? "").Trim().ToLower();
            var skip = Math.Max(0, request.Skip);
            var take = Math.Clamp(request.Take <= 0 ? 8 : request.Take, 1, MaxSuggested);
            if (skip >= MaxSuggested)
                return new Result<List<ItemCard>>(HttpStatusCode.OK, new List<ItemCard>(), null);

            take = Math.Min(take, MaxSuggested - skip);

            var items = await MealMenu.VisibleItems(db, currentUser, cancellationToken);
            if (request.CuisineId is Guid cuisineId)
                items = items.Where(e => e.CuisineId == cuisineId).ToList();

            var searchView = await MenuDialect.For(db, currentUser.UserId, cancellationToken);
            items = await ItemSearch.Filter(db, items, searchView, new ItemSearchOptions(request.Text, request.CategoryId, request.Ingredient, request.KitchenId, request.GroupId, request.FilterMealId, request.CategoryIds), cancellationToken);

            // Dishes of the country the customer is in first, then its neighbors (the mix stays inside each part).
            var (homeCuisines, nearCuisines) = await CuisineOrder.Ranked(db, searchView, cancellationToken);
            items = SuggestMix.Interleave(items)
                .OrderBy(e => e.CuisineId is Guid c ? (homeCuisines.Contains(c) ? 0 : nearCuisines.Contains(c) ? 1 : 2) : 2)
                .Take(MaxSuggested).ToList();
            items = items.Skip(skip).Take(take).ToList();

            return new Result<List<ItemCard>>(HttpStatusCode.OK,
                await ItemCards.Build(db, currentUser, items, "", 0, null, cancellationToken, preserveOrder: true, null, request.CuisineId ?? request.KitchenId), null);
        }
    }


    file static class SuggestMix
    {
        public static List<Foods.Domain.Entity.MasterData.Item.Item> Interleave(
            List<Foods.Domain.Entity.MasterData.Item.Item> items)
        {
            var groups = items
                .GroupBy(e => e.CuisineId)
                .OrderBy(g => g.Key.HasValue ? 0 : 1)
                .ThenBy(g => g.Min(e => e.Name))
                .Select(g => g.OrderBy(e => e.Sort).ThenBy(e => e.Name).ToList())
                .ToList();
            if (groups.Count <= 1)
                return items.OrderBy(e => e.Sort).ThenBy(e => e.Name).ToList();

            var mixed = new List<Foods.Domain.Entity.MasterData.Item.Item>(items.Count);
            var index = 0;
            var added = true;
            while (added)
            {
                added = false;
                foreach (var group in groups)
                {
                    if (index >= group.Count) continue;
                    mixed.Add(group[index]);
                    added = true;
                }
                index++;
            }
            return mixed;
        }
    }

    public sealed class GetItemsQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<GetItemsQuery, List<ItemCard>>
    {
        public async Task<Result<List<ItemCard>>> Handle(GetItemsQuery request, CancellationToken cancellationToken)
        {
            var text = (request.Text ?? "").Trim();
            var skip = Math.Max(0, request.Skip);
            var take = request.Take is int value && value > 0 ? Math.Clamp(value, 1, 50) : 20;

            var view = await MenuDialect.For(db, currentUser.UserId, cancellationToken);
            if (!view.ShowAll && view.CountryId == null)
                return new Result<List<ItemCard>>(HttpStatusCode.OK, new List<ItemCard>(), null);

            var query = MealMenu.QueryFor(db, request.MealId, view);
            if (request.CuisineId is Guid cuisineId)
                query = query.Where(e => e.CuisineId == cuisineId);

            var search = new ItemSearchOptions(text, request.CategoryId, request.Ingredient, request.KitchenId, request.GroupId, request.FilterMealId, request.CategoryIds);
            if (!search.IsEmpty)
            {
                var all = await query.OrderBy(e => e.Sort).ThenBy(e => e.Name).ToListAsync(cancellationToken);
                var found = await ItemSearch.Filter(db, all, view, search, cancellationToken);
                var (homeHit, nearHit) = await CuisineOrder.Ranked(db, view, cancellationToken);
                var hit = found.OrderBy(e => e.CuisineId is Guid c ? (homeHit.Contains(c) ? 0 : nearHit.Contains(c) ? 1 : 2) : 2).ThenBy(e => e.Sort)
                    .Skip(skip).Take(take).ToList();
                return new Result<List<ItemCard>>(HttpStatusCode.OK,
                    await ItemCards.Build(db, currentUser, hit, "", 0, null, cancellationToken, preserveOrder: true, view, request.CuisineId ?? request.KitchenId), null);
            }

            if (text.Length > 0)
            {
                var needle = text.ToLower();
                if (view.CountryId is Guid countryId)
                {
                    query = query.Where(e =>
                        (e.Name != null && e.Name.ToLower().Contains(needle)) ||
                        (e.Summary != null && e.Summary.ToLower().Contains(needle)) ||
                        db.ItemCountries.Any(link =>
                            link.ItemId == e.Id &&
                            link.CountryId == countryId &&
                            !link.IsDeleted &&
                            ((link.Name != null && link.Name.ToLower().Contains(needle)) ||
                             (link.Summary != null && link.Summary.ToLower().Contains(needle)))));
                }
                else
                {
                    query = query.Where(e =>
                        (e.Name != null && e.Name.ToLower().Contains(needle)) ||
                        (e.Summary != null && e.Summary.ToLower().Contains(needle)));
                }
            }

            // The country the customer is in first, then its neighbors, then the rest.
            var (homeIds, nearIds) = await CuisineOrder.Ranked(db, view, cancellationToken);
            var page = await query
                .OrderBy(e => e.CuisineId != null && homeIds.Contains(e.CuisineId.Value) ? 0 : e.CuisineId != null && nearIds.Contains(e.CuisineId.Value) ? 1 : 2)
                .ThenBy(e => e.Sort)
                .ThenBy(e => e.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return new Result<List<ItemCard>>(HttpStatusCode.OK,
                await ItemCards.Build(db, currentUser, page, "", 0, null, cancellationToken, preserveOrder: true, view, request.CuisineId ?? request.KitchenId), null);
        }
    }

    file static class CuisineFilters
    {
        public static async Task<List<MealItemFilter>> Build(
            IFoodsDbContext db,
            List<Foods.Domain.Entity.MasterData.Item.Item> items,
            CancellationToken cancellationToken,
            MenuView? view = null)
        {
            var cuisineIds = items.Where(e => e.CuisineId != null).Select(e => e.CuisineId!.Value).Distinct().ToList();
            // Cuisines of the customer's own country first.
            var all = await db.Cuisines.Where(e => !e.IsDeleted).ToListAsync(cancellationToken);
            var cuisines = view == null ? all.OrderBy(e => e.Sort).ThenBy(e => e.Name).ToList() : CuisineOrder.Sort(all, view);

            var filters = new List<MealItemFilter>
            {
                new(null, "الكل", items.Count, null, null, null)
            };
            filters.AddRange(cuisines.Select(cuisine =>
                new MealItemFilter(
                    cuisine.Id,
                    cuisine.Name ?? "",
                    items.Count(item => item.CuisineId == cuisine.Id),
                    cuisine.IconPath,
                    cuisine.Color,
                    cuisine.TextColor,
                    cuisine.ImagePath,
                    view != null && CuisineOrder.IsMine(cuisine, view))));
            return filters;
        }
    }

    file static class ItemCards
    {
        public static async Task<List<ItemCard>> Build(
            IFoodsDbContext db,
            ICurrentUser currentUser,
            List<Foods.Domain.Entity.MasterData.Item.Item> menuItems,
            string text,
            int skip,
            int? limit,
            CancellationToken cancellationToken,
            bool preserveOrder = false,
            MenuView? view = null,
            Guid? cuisineId = null)
        {
            view ??= await MenuDialect.For(db, currentUser.UserId, cancellationToken);
            // Picked a country's cuisine: prices follow that country's version of the dishes.
            var scope = cuisineId == null ? null
                : new RecipeCountries(await CountryVersions.ForCuisine(db, currentUser.UserId, cuisineId, cancellationToken));
            if (menuItems.Count == 0)
                return new List<ItemCard>();

            var pageIds = menuItems.Select(e => e.Id).ToList();
            var favoriteIds = await db.Set<Favorite>()
                .Where(e => e.UserId == currentUser.UserId && !e.IsDeleted && pageIds.Contains(e.ItemId))
                .Select(e => e.ItemId)
                .ToListAsync(cancellationToken);
            var favoriteSet = favoriteIds.ToHashSet();

            var spoken = view.CountryId is Guid countryId
                ? await MenuDialect.ForItems(db, countryId, pageIds, cancellationToken)
                : new Dictionary<Guid, Foods.Domain.Entity.MasterData.Item.ItemCountry>();

            var categoryIds = menuItems.Select(e => e.ItemCategoryId).Distinct().ToList();
            var categories = await db.ItemCategories
                .Where(e => categoryIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);

            var groupIds = menuItems.Select(e => e.ItemGroupId).Distinct().ToList();
            var groups = await db.ItemGroups
                .Where(e => groupIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);

            var cuisineIds = menuItems.Where(e => e.CuisineId != null).Select(e => e.CuisineId!.Value).Distinct().ToList();
            var cuisines = cuisineIds.Count == 0
                ? new Dictionary<Guid, string?>()
                : await db.Cuisines
                    .Where(e => cuisineIds.Contains(e.Id))
                    .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);

            IEnumerable<Foods.Domain.Entity.MasterData.Item.Item> filtered = menuItems;
            if (text.Length > 0)
            {
                var needle = text.ToLower();
                filtered = filtered.Where(e =>
                {
                    spoken.TryGetValue(e.Id, out var local);
                    var name = MenuDialect.Pick(local?.Name, e.Name) ?? "";
                    var summary = MenuDialect.Pick(local?.Summary, e.Summary) ?? "";
                    return name.ToLower().Contains(needle) || summary.ToLower().Contains(needle);
                });
            }
            if (!preserveOrder)
                filtered = filtered.OrderBy(e => MenuDialect.Pick(spoken.GetValueOrDefault(e.Id)?.Name, e.Name));

            if (skip > 0)
                filtered = filtered.Skip(skip);
            if (limit is int take)
                filtered = filtered.Take(take);

            var page = filtered.ToList();
            var albums = await AlbumImages.ForItems(db, page.Select(e => (e.Id, e.ImagePath)), cancellationToken);
            var book = await Foods.Application.Customer.Pricing.PriceBook.For(db, currentUser.UserId, cancellationToken);
            var prices = book == null ? new() : await book.Dishes(page.Select(e => e.Id).ToList(), cancellationToken, scope);

            return page
                .Select(e =>
                {
                    spoken.TryGetValue(e.Id, out var local);
                    categories.TryGetValue(e.ItemCategoryId, out var categoryName);
                    string? cuisineName = null;
                    if (e.CuisineId is Guid cuisineId)
                        cuisines.TryGetValue(cuisineId, out cuisineName);
                    return new ItemCard(
                        e.Id,
                        MenuDialect.Pick(local?.Name, e.Name),
                        MenuDialect.Pick(local?.Summary, e.Summary),
                        e.ImagePath,
                        favoriteSet.Contains(e.Id),
                        view.CountryName,
                        view.Dialect,
                        e.ItemCategoryId,
                        categoryName,
                        e.CuisineId,
                        cuisineName,
                        AlbumImages.Of(albums, e.Id, e.ImagePath),
                        e.ItemGroupId,
                        groups.GetValueOrDefault(e.ItemGroupId),
                        book == null ? null : prices.GetValueOrDefault(e.Id)?.Total ?? 0,
                        prices.GetValueOrDefault(e.Id)?.Missing ?? 0,
                        // On another country's cuisine: picking the dish takes that country's version.
                        scope?.Default != null && scope.Default != view.LiveId ? scope.Default : null);
                })
                .ToList();
        }
    }

    file static class MealMenu
    {
        public static IQueryable<Foods.Domain.Entity.MasterData.Item.Item> QueryFor(
            IFoodsDbContext db,
            Guid? mealId,
            MenuView view)
        {
            IQueryable<Foods.Domain.Entity.MasterData.Item.Item> query = mealId is Guid id
                ? from link in db.ItemMeals
                  join item in db.Items on link.ItemId equals item.Id
                  where link.MealId == id && !link.IsDeleted && !item.IsDeleted && item.ShowToCustomer
                  select item
                : db.Items.Where(item => !item.IsDeleted && item.ShowToCustomer);

            if (view.ShowAll)
                return query;

            // Dishes offered in the country the customer is in.
            if (view.LiveId is not Guid countryId)
                return query.Where(_ => false);

            return query.Where(item =>
                db.ItemCountries.Any(link =>
                    link.ItemId == item.Id &&
                    link.CountryId == countryId &&
                    !link.IsDeleted));
        }

        public static async Task<List<Foods.Domain.Entity.MasterData.Item.Item>> ItemsFor(
            IFoodsDbContext db,
            ICurrentUser currentUser,
            Guid mealId,
            CancellationToken cancellationToken)
        {
            var view = await MenuDialect.For(db, currentUser.UserId, cancellationToken);
            return await QueryFor(db, mealId, view).OrderBy(e => e.Sort).ThenBy(e => e.Name).ToListAsync(cancellationToken);
        }

        public static async Task<List<Foods.Domain.Entity.MasterData.Item.Item>> VisibleItems(
            IFoodsDbContext db,
            ICurrentUser currentUser,
            CancellationToken cancellationToken)
        {
            var view = await MenuDialect.For(db, currentUser.UserId, cancellationToken);
            var query = db.Items.Where(e => !e.IsDeleted && e.ShowToCustomer);
            if (view.ShowAll)
                return await query.OrderBy(e => e.Sort).ThenBy(e => e.Name).ToListAsync(cancellationToken);
            if (view.LiveId is not Guid countryId)
                return new();
            return await query
                .Where(item => db.ItemCountries.Any(link =>
                    link.ItemId == item.Id && link.CountryId == countryId && !link.IsDeleted))
                .OrderBy(e => e.Sort).ThenBy(e => e.Name)
                .ToListAsync(cancellationToken);
        }
    }

    /// <summary>The dishes the customer can see (their country, shown to customers): used for category and group counts.</summary>
    public static class CustomerMenu
    {
        public static Task<List<Foods.Domain.Entity.MasterData.Item.Item>> Visible(IFoodsDbContext db, ICurrentUser currentUser, CancellationToken cancellationToken)
            => MealMenu.VisibleItems(db, currentUser, cancellationToken);
    }

    /// <param name="CuisineId">The cuisine the customer opened the dish from: its country's version is shown.</param>
    public sealed record GetItemQuery(Guid Id, Guid? CuisineId = null, Guid? CountryId = null) : IQuery<ItemDetail>;
    public sealed record ItemIngredient(Guid ContentId, string? Name, string? ImagePath, Guid UnitId, string? UnitName, decimal Quantity, Guid? PurchasePlaceId, string? PurchasePlaceName, List<string>? Images = null, bool IsAdded = false, bool IsChanged = false, bool IsOptional = false, bool Selected = true,
        decimal? Cost = null, Foods.Application.Customer.Pricing.PriceInfo? PriceInfo = null);
    public sealed record ItemStepDto(int Sort, string? Body);
    public sealed record ItemVideoDto(int Sort, string? Title, string? Url);
    public sealed record ItemDetail(
        Guid Id,
        string? Name,
        string? Summary,
        string? Description,
        string? ImagePath,
        bool IsFavorite,
        string? CountryName,
        string? Dialect,
        List<ItemIngredient> Ingredients,
        List<ItemStepDto> Steps,
        List<ItemVideoDto> Videos,
        List<string>? Images = null,
        bool IsCustomized = false,
        string? GroupName = null,
        string? CategoryName = null,
        List<ItemPartView>? Parts = null,
        bool ShowToCustomer = true,
        Foods.Application.Customer.Pricing.DishPrice? Price = null,
        Guid? RecipeCountryId = null,
        string? RecipeCountryName = null,
        bool IsOtherCountry = false);

    /// <summary>A part (صنف) of a dish for the customer: another dish that is required, or optional with the customer's pick.</summary>
    public sealed record ItemPartView(Guid ItemId, string? Name, string? Summary, string? ImagePath, bool IsOptional, bool Selected, bool CanOpen, int IngredientCount, bool IsAdded = false);

    public sealed class GetItemQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<GetItemQuery, ItemDetail>
    {
        public async Task<Result<ItemDetail>> Handle(GetItemQuery request, CancellationToken cancellationToken)
        {
            var item = await db.Items.FirstOrDefaultAsync(e => e.Id == request.Id && !e.IsDeleted, cancellationToken);
            if (item == null)
                return new Result<ItemDetail>(HttpStatusCode.NotFound, null, new List<string> { "الأكلة غير موجودة" });

            var view = await MenuDialect.For(db, currentUser.UserId, cancellationToken);
            Foods.Domain.Entity.MasterData.Item.ItemCountry? spoken = null;
            if (view.CountryId is Guid countryId)
            {
                var links = await MenuDialect.ForItems(db, countryId, new[] { item.Id }, cancellationToken);
                links.TryGetValue(item.Id, out spoken);
            }
            if (!view.ShowAll && !(await MenuDialect.Offered(db, view, new[] { item.Id }, cancellationToken)).Contains(item.Id))
                return new Result<ItemDetail>(HttpStatusCode.NotFound, null, new List<string> { view.CountryId == null ? "حدد بلدك الأول" : "الأكلة مش متاحة في بلدك" });

            // Which country's version: the picked cuisine's country (or the version the client already shows), else the customer's.
            var recipeCountry = request.CountryId is Guid shown && shown != Guid.Empty
                ? await CountryVersions.Check(db, currentUser.UserId, shown, cancellationToken)
                : await CountryVersions.ForCuisine(db, currentUser.UserId, request.CuisineId, cancellationToken);
            var scope = RecipeCountries.Of(recipeCountry, item.Id);
            // The customer's own version of the dish (نسختك الخاصة) when they changed it.
            var lines = await PersonalRecipe.Lines(db, currentUser.UserId, new[] { item.Id }, cancellationToken, scope);
            var (steps, _) = await PersonalRecipe.Steps(db, currentUser.UserId, item.Id, cancellationToken, recipeCountry);
            var customized = await PersonalRecipe.IsCustomized(db, currentUser.UserId, item.Id, cancellationToken, recipeCountry);
            var videos = await PersonalRecipe.Videos(db, item.Id, recipeCountry, cancellationToken);
            var favorite = await db.Set<Favorite>().AnyAsync(e => e.UserId == currentUser.UserId && e.ItemId == item.Id && !e.IsDeleted, cancellationToken);
            var itemAlbum = await AlbumImages.ForItems(db, new[] { (item.Id, item.ImagePath) }, cancellationToken);

            var choices = await PersonalRecipe.Choices(db, currentUser.UserId, new[] { item.Id }, cancellationToken, scope);
            var ingredients = (await PersonalRecipe.Ingredients(db, lines, cancellationToken, view.CountryId))
                .Select(e => e.IsOptional
                    ? e with { Selected = PersonalRecipe.IsSelected(choices, item.Id, Foods.Domain.Entity.MasterData.Item.UserItemChoice.ContentKind, e.ContentId, true, e.Selected) }
                    : e)
                .ToList();
            // A required part the customer dropped in "تعديل الأكلة ليا" is not shown.
            var parts = await PersonalRecipe.PartViews(db, currentUser.UserId, item.Id, view, false, cancellationToken, recipeCountry);
            var groupName = await db.ItemGroups.Where(e => e.Id == item.ItemGroupId).Select(e => e.Name).FirstOrDefaultAsync(cancellationToken);
            var categoryName = await db.ItemCategories.Where(e => e.Id == item.ItemCategoryId).Select(e => e.Name).FirstOrDefaultAsync(cancellationToken);
            // Prices (when the customer works with prices): each ingredient's cost per person and the dish price for the family.
            var book = await Foods.Application.Customer.Pricing.PriceBook.For(db, currentUser.UserId, cancellationToken);
            Foods.Application.Customer.Pricing.DishPrice? price = null;
            if (book != null)
            {
                var dishPrices = await book.Dishes(new[] { item.Id }, cancellationToken, scope);
                price = dishPrices.GetValueOrDefault(item.Id) ?? new Foods.Application.Customer.Pricing.DishPrice(0, 0, book.Household, 0);
                await book.Load(ingredients.Select(e => e.ContentId), cancellationToken);
                ingredients = ingredients.Select(e => e with { Cost = book.Cost(e.ContentId, e.UnitId, e.Quantity), PriceInfo = book.Info(e.ContentId) }).ToList();
            }

            var detail = new ItemDetail(
                item.Id,
                MenuDialect.Pick(spoken?.Name, item.Name),
                MenuDialect.Pick(spoken?.Summary, item.Summary),
                MenuDialect.Pick(spoken?.Description, item.Description),
                item.ImagePath,
                favorite,
                view.CountryName,
                view.Dialect,
                ingredients,
                steps,
                videos.Select(e => new ItemVideoDto(e.Sort, e.Title, e.Url)).ToList(),
                AlbumImages.Of(itemAlbum, item.Id, item.ImagePath),
                customized,
                groupName,
                categoryName,
                parts,
                item.ShowToCustomer,
                price,
                recipeCountry,
                await CountryVersions.CountryName(db, recipeCountry, cancellationToken),
                recipeCountry != null && recipeCountry != view.LiveId);

            return new Result<ItemDetail>(HttpStatusCode.OK, detail, null);
        }
    }

    // ---------------- PUT customer/items/{id}/choice ----------------

    /// <summary>Picks or drops one optional ingredient (Kind 0, RefId = ContentId) or optional part (Kind 1, RefId = part dish id).</summary>
    public sealed record SetItemChoiceCommand(Guid ItemId, int Kind, Guid RefId, bool Selected, Guid? CountryId = null) : Foods.Application.Abstraction.Command.ICommand<bool>;

    public sealed class SetItemChoiceCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : Foods.Application.Abstraction.Command.ICommandHandler<SetItemChoiceCommand, bool>
    {
        public async Task<Result<bool>> Handle(SetItemChoiceCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId;
            if (userId == Guid.Empty)
                return new Result<bool>(HttpStatusCode.Unauthorized, false, new List<string> { "سجل دخول الأول" });

            var country = await CountryVersions.Check(db, userId, request.CountryId, cancellationToken);
            bool isDefault;
            if (request.Kind == Foods.Domain.Entity.MasterData.Item.UserItemChoice.ContentKind)
            {
                var line = (await PersonalRecipe.BaseLines(db, request.ItemId, country, cancellationToken)).FirstOrDefault(e => e.ContentId == request.RefId && e.IsOptional);
                if (line == null)
                    return new Result<bool>(HttpStatusCode.BadRequest, false, new List<string> { "المكوّن ده أساسي ومينفعش يتشال" });
                isDefault = line.IsDefault;
            }
            else if (request.Kind == Foods.Domain.Entity.MasterData.Item.UserItemChoice.PartKind)
            {
                var part = (await PersonalRecipe.BaseParts(db, request.ItemId, country, cancellationToken)).FirstOrDefault(e => e.PartItemId == request.RefId && e.IsOptional);
                if (part == null)
                    return new Result<bool>(HttpStatusCode.BadRequest, false, new List<string> { "الصنف ده أساسي ومينفعش يتشال" });
                isDefault = part.IsDefault;
            }
            else
                return new Result<bool>(HttpStatusCode.BadRequest, false, new List<string> { "نوع غير معروف" });

            var old = await db.UserItemChoices
                .Where(e => e.UserId == userId && e.ItemId == request.ItemId && e.Kind == request.Kind && e.RefId == request.RefId && !e.IsDeleted && e.CountryId == country)
                .ToListAsync(cancellationToken);
            foreach (var row in old) row.IsDeleted = true;
            // Only differences from the dish default are stored.
            if (request.Selected != isDefault)
            {
                db.UserItemChoices.Add(new Foods.Domain.Entity.MasterData.Item.UserItemChoice
                {
                    Id = Guid.NewGuid(), UserId = userId, ItemId = request.ItemId, Kind = request.Kind,
                    RefId = request.RefId, Selected = request.Selected, CountryId = country, IsDeleted = false, Status = 0
                });
            }
            await db.SaveChangesAsync(cancellationToken);
            return new Result<bool>(HttpStatusCode.OK, request.Selected, null);
        }
    }
}
