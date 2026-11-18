namespace Foods.Application.Customer.Ordering
{
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.Request;
    using Foods.Application.Abstraction.Query;
    using Foods.Application.Abstraction.Command;
    using Foods.Application.Abstraction.Security;
    using Foods.Application.Customer.Catalog;

    public sealed record CalculateOrderQuery(IReadOnlyList<ItemSelection> Items) : IQuery<List<ShoppingLineResult>>;

    public sealed class CalculateOrderQueryHandler(IOrderCalculator calculator, ICurrentUser currentUser) : IQueryHandler<CalculateOrderQuery, List<ShoppingLineResult>>
    {
        public async Task<Result<List<ShoppingLineResult>>> Handle(CalculateOrderQuery request, CancellationToken cancellationToken)
        {
            var lines = await calculator.CalculateAsync(currentUser.UserId, request.Items ?? Array.Empty<ItemSelection>(), cancellationToken);
            return new Result<List<ShoppingLineResult>>(HttpStatusCode.OK, lines.ToList(), null);
        }
    }

    /// <summary>0 = draft, 1 = temporary save (حفظ مؤقت), 2 = confirmed purchase order (محفوظ).</summary>
    public static class OrderStatuses
    {
        public const int Draft = 0;
        public const int Temporary = 1;
        public const int Confirmed = 2;
        public const string DraftName = "طلباتي";
    }

    public sealed record OrderListItem(Guid Id, string? Name, DateTime Date, int Status);
    public sealed record OrderItem(Guid ItemId, string? Name, string? ImagePath, decimal RepeatCount, int? PeopleCount, decimal? Price = null, Guid? CountryId = null, string? CountryName = null);
    /// <param name="Total">Money of the whole order from the current ingredient prices (null when prices are off).</param>
    /// <param name="PriceMissing">Lines with no price yet.</param>
    public sealed record OrderDetail(Guid Id, string? Name, DateTime Date, string? Notes, int Status, List<OrderItem> Items, List<ShoppingLineResult> Lines, decimal? Total = null, int PriceMissing = 0);

    public sealed record GetOrdersQuery() : IQuery<List<OrderListItem>>;
    public sealed record GetOrderQuery(Guid Id) : IQuery<OrderDetail>;
    public sealed record SaveOrderCommand(Guid? Id, string Name, DateTime Date, string? Notes, IReadOnlyList<ItemSelection> Items, IReadOnlyList<ShoppingLineInput>? Lines, bool Confirm = false) : ICommand<Guid>;
    public sealed record ReorderCommand(Guid Id) : ICommand<Guid>;
    public sealed record RenameOrderCommand(Guid Id, string Name) : ICommand<Guid>;

    /// <summary>
    /// Default name of a confirmed purchase order: the month's name, a running number when the
    /// month already has confirmed orders, and the date — e.g. "طلب نوفمبر 2 - 11/11/2026".
    /// </summary>
    public static class OrderNaming
    {
        private static readonly string[] Months =
        {
            "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
            "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
        };

        /// <summary>Names the client sends when the user did not choose one.</summary>
        public static bool IsDefault(string? name) =>
            string.IsNullOrWhiteSpace(name) || name.Trim().StartsWith("طلب شراء");

        public static async Task<string> NextAsync(IFoodsDbContext db, Guid userId, DateTime date, CancellationToken cancellationToken)
        {
            var start = new DateTime(date.Year, date.Month, 1);
            var end = start.AddMonths(1);
            var before = await db.Set<Request>().CountAsync(e =>
                e.UserId == userId && !e.IsDeleted && e.Status == OrderStatuses.Confirmed &&
                e.Date >= start && e.Date < end, cancellationToken);
            var number = before + 1;
            var month = Months[date.Month - 1];
            return $"طلب {month}{(number > 1 ? " " + number : "")} - {date:d/M/yyyy}";
        }
    }

    public sealed class GetOrdersQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<GetOrdersQuery, List<OrderListItem>>
    {
        public async Task<Result<List<OrderListItem>>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
        {
            var orders = await db.Set<Request>()
                .Where(e => e.UserId == currentUser.UserId && !e.IsDeleted)
                .OrderByDescending(e => e.Date)
                .Select(e => new OrderListItem(e.Id, e.Name, e.Date, e.Status))
                .ToListAsync(cancellationToken);

            return new Result<List<OrderListItem>>(HttpStatusCode.OK, orders, null);
        }
    }

    public sealed class GetOrderQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<GetOrderQuery, OrderDetail>
    {
        public async Task<Result<OrderDetail>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
        {
            var order = await Load(db, currentUser.UserId, request.Id, cancellationToken);
            if (order == null)
                return new Result<OrderDetail>(HttpStatusCode.NotFound, null, new List<string> { "الطلب غير موجود" });

            return new Result<OrderDetail>(HttpStatusCode.OK, order, null);
        }

        public static async Task<OrderDetail?> Load(IFoodsDbContext db, Guid userId, Guid id, CancellationToken cancellationToken)
        {
            var order = await db.Set<Request>().FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId && !e.IsDeleted, cancellationToken);
            if (order == null)
                return null;

            var items = await db.Set<RequestItem>().Where(e => e.RequestId == order.Id && !e.IsDeleted).ToListAsync(cancellationToken);
            var itemIds = items.Select(e => e.ItemId).Distinct().ToList();
            var menuItems = await db.Items.Where(e => itemIds.Contains(e.Id)).ToListAsync(cancellationToken);
            var lines = await db.Set<RequestShoppingLine>().Where(e => e.RequestId == order.Id && !e.IsDeleted).ToListAsync(cancellationToken);
            var contentIds = lines.Select(e => e.ContentId).Distinct().ToList();
            var contents = await db.Contents.Where(e => contentIds.Contains(e.Id)).ToListAsync(cancellationToken);
            var unitIds = lines.Select(e => e.UnitId).Distinct().ToList();
            var units = await db.Units.Where(e => unitIds.Contains(e.Id)).ToListAsync(cancellationToken);
            var placeIds = lines.Where(e => e.PurchasePlaceId != null).Select(e => e.PurchasePlaceId!.Value).Distinct().ToList();
            var places = await db.PurchasePlaces.Where(e => placeIds.Contains(e.Id)).ToListAsync(cancellationToken);
            var albums = await AlbumImages.ForContents(db, contents.Select(e => (e.Id, e.ImagePath)), cancellationToken);

            var view = await MenuDialect.For(db, userId, cancellationToken);
            var spoken = view.CountryId is Guid countryId
                ? await MenuDialect.ForItems(db, countryId, itemIds, cancellationToken)
                : new Dictionary<Guid, Foods.Domain.Entity.MasterData.Item.ItemCountry>();

            var scope = await ItemSelection.Scope(db, userId, items.Select(e => new ItemSelection(e.ItemId, e.RepeatCount, e.PeopleCount, e.CountryId)), cancellationToken);
            var contentNames = await CountryVersions.ContentsIn(db, view.CountryId, contentIds, cancellationToken);
            var otherCountries = items.Where(e => e.CountryId != null && e.CountryId != view.LiveId).Select(e => e.CountryId!.Value).Distinct().ToList();
            var countryNames = await db.Countries.Where(e => otherCountries.Contains(e.Id)).ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);
            var book = await Foods.Application.Customer.Pricing.PriceBook.For(db, userId, cancellationToken);
            Dictionary<Guid, Foods.Application.Customer.Pricing.DishPrice> dishPrices = new();
            if (book != null)
            {
                await book.Load(contentIds, cancellationToken);
                dishPrices = await book.Dishes(itemIds, cancellationToken, scope);
            }

            var detail = new OrderDetail(
                order.Id,
                order.Name,
                order.Date,
                order.Notes,
                order.Status,
                items
                    .GroupBy(item => item.ItemId)
                    .Select(group =>
                    {
                        var item = group.OrderByDescending(e => e.RepeatCount == 0 ? e.Quantity : e.RepeatCount).First();
                        var menuItem = menuItems.FirstOrDefault(e => e.Id == item.ItemId);
                        spoken.TryGetValue(item.ItemId, out var local);
                        var repeat = item.RepeatCount == 0 ? item.Quantity : item.RepeatCount;
                        decimal? price = null;
                        if (book != null && dishPrices.TryGetValue(item.ItemId, out var dish))
                        {
                            var people = item.PeopleCount is int count && count > 0 ? count : book.Household;
                            price = Foods.Application.Customer.Pricing.PriceBook.Round(dish.PerPerson * people * (repeat <= 0 ? 1 : repeat));
                        }
                        var otherCountry = item.CountryId is Guid c && countryNames.TryGetValue(c, out var cn) ? cn : null;
                        return new OrderItem(item.ItemId, MenuDialect.Pick(local?.Name, menuItem?.Name), menuItem?.ImagePath, repeat, item.PeopleCount, price, item.CountryId, otherCountry);
                    }).ToList(),
                lines.Select(line =>
                {
                    var content = contents.FirstOrDefault(e => e.Id == line.ContentId);
                    var result = new ShoppingLineResult(
                        line.ContentId,
                        CountryVersions.Name(contentNames, line.ContentId, content?.Name),
                        content?.ImagePath,
                        line.PurchasePlaceId,
                        places.FirstOrDefault(e => e.Id == line.PurchasePlaceId)?.Name,
                        line.UnitId,
                        units.FirstOrDefault(e => e.Id == line.UnitId)?.Name,
                        line.Quantity,
                        content?.PurchaseMode ?? 0,
                        content?.PackageName,
                        PurchaseRules.Step(content?.PurchaseMode ?? 0, content?.PurchaseStep ?? 0),
                        content?.PurchaseIncrement ?? 0,
                        AlbumImages.Of(albums, line.ContentId, content?.ImagePath),
                        content?.PurchaseUnitPrice ?? 0,
                        content?.PurchaseMinAmount ?? 0);
                    return book == null ? result : book.Priced(result);
                })
                // Admin order: places by their sort, then ingredients by their sort, then names.
                .OrderBy(e => e.PurchasePlaceId is Guid pid ? (places.FirstOrDefault(p => p.Id == pid)?.Sort ?? 0) : int.MinValue)
                .ThenBy(e => e.PurchasePlaceName)
                .ThenBy(e => contents.FirstOrDefault(c => c.Id == e.ContentId)?.Sort ?? 0)
                .ThenBy(e => e.Name).ToList());
            if (book == null) return detail;
            var priced = detail.Lines.Where(e => e.Quantity > 0).ToList();
            return detail with
            {
                Total = Foods.Application.Customer.Pricing.PriceBook.Round(priced.Sum(e => e.Price ?? 0)),
                PriceMissing = priced.Count(e => e.Price == null)
            };
        }
    }

    public sealed class SaveOrderCommandHandler(IFoodsDbContext db, ICurrentUser currentUser, IOrderCalculator calculator) : ICommandHandler<SaveOrderCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(SaveOrderCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name) && !request.Confirm)
                return new Result<Guid>(HttpStatusCode.BadRequest, Guid.Empty, new List<string> { "اكتب اسم الطلب" });

            // Empty selection: soft-delete the existing draft/temp order (used by "طلباتي" clear).
            if (request.Items == null || request.Items.Count == 0)
            {
                if (request.Id is not Guid clearId || clearId == Guid.Empty)
                    return new Result<Guid>(HttpStatusCode.BadRequest, Guid.Empty, new List<string> { "اختر أكلة واحدة على الأقل" });

                var clearOrder = await db.Set<Request>().FirstOrDefaultAsync(e => e.Id == clearId && e.UserId == currentUser.UserId && !e.IsDeleted, cancellationToken);
                if (clearOrder == null)
                    return new Result<Guid>(HttpStatusCode.NotFound, Guid.Empty, new List<string> { "الطلب غير موجود" });

                await MarkChildrenDeleted(db, clearOrder.Id, cancellationToken);
                clearOrder.IsDeleted = true;
                await db.SaveChangesAsync(cancellationToken);
                return new Result<Guid>(HttpStatusCode.OK, clearOrder.Id, null);
            }

            // One row per item — avoid duplicate RequestItem rows for the same ItemId.
            var uniqueItems = request.Items
                .GroupBy(e => e.ItemId)
                .Select(g => g.OrderByDescending(e => e.RepeatCount).First())
                .ToList();

            Request order;
            var name = request.Name?.Trim() ?? string.Empty;
            // Confirm = حفظ الطلب: always create a new confirmed order (separate from the temp draft).
            if (request.Confirm)
            {
                if (OrderNaming.IsDefault(name))
                    name = await OrderNaming.NextAsync(db, currentUser.UserId, request.Date, cancellationToken);
                order = new Request
                {
                    Id = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    Date = request.Date,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false,
                    Status = OrderStatuses.Confirmed
                };
                db.Set<Request>().Add(order);
            }
            else if (request.Id is Guid id && id != Guid.Empty)
            {
                var existing = await db.Set<Request>().FirstOrDefaultAsync(e => e.Id == id && e.UserId == currentUser.UserId && !e.IsDeleted, cancellationToken);
                if (existing == null)
                    return new Result<Guid>(HttpStatusCode.NotFound, Guid.Empty, new List<string> { "الطلب غير موجود" });
                order = existing;
                await MarkChildrenDeleted(db, order.Id, cancellationToken);
                // Selecting meals keeps the working order as temporary save.
                order.Status = OrderStatuses.Temporary;
            }
            else
            {
                order = new Request
                {
                    Id = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    Date = request.Date,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false,
                    Status = OrderStatuses.Temporary
                };
                db.Set<Request>().Add(order);
            }

            order.Name = name;
            order.Date = request.Date;
            order.Notes = request.Notes;
            order.Code = order.Date.ToString("yyyyMMdd");
            if (request.Confirm)
                order.Status = OrderStatuses.Confirmed;

            var calculated = await calculator.CalculateAsync(currentUser.UserId, uniqueItems, cancellationToken);
            var lines = request.Lines is { Count: > 0 }
                ? request.Lines.Where(e => e.Quantity > 0).Select(e =>
                {
                    var known = calculated.FirstOrDefault(c => c.ContentId == e.ContentId && c.UnitId == e.UnitId);
                    return new ShoppingLineResult(e.ContentId, known?.Name, known?.ImagePath, known?.PurchasePlaceId, known?.PurchasePlaceName, e.UnitId, known?.UnitName, e.Quantity);
                }).ToList()
                : calculated.ToList();

            // Batch recipe + people-factor lookups (avoids N+1 queries per meal).
            var itemIds = uniqueItems.Select(e => e.ItemId).Distinct().ToList();
            var recipeRows = await PersonalRecipe.ForOrder(db, currentUser.UserId, itemIds, cancellationToken, await ItemSelection.Scope(db, currentUser.UserId, uniqueItems, cancellationToken));
            var recipesByItem = recipeRows
                .GroupBy(e => e.ItemId)
                .ToDictionary(g => g.Key, g => g.ToList());
            // Dictionary keys cannot be null — map missing PeopleCount to a sentinel.
            var peopleCache = new Dictionary<int, decimal>();

            async Task<decimal> PeopleAsync(int? peopleCount)
            {
                var key = peopleCount ?? int.MinValue;
                if (peopleCache.TryGetValue(key, out var cached)) return cached;
                var value = await calculator.PeopleFactorAsync(currentUser.UserId, peopleCount, cancellationToken);
                peopleCache[key] = value;
                return value;
            }

            foreach (var item in uniqueItems)
            {
                var repeat = item.RepeatCount <= 0 ? 1 : item.RepeatCount;
                var requestItem = new RequestItem
                {
                    Id = Guid.NewGuid(),
                    RequestId = order.Id,
                    ItemId = item.ItemId,
                    Quantity = repeat,
                    RepeatCount = repeat,
                    PeopleCount = item.PeopleCount,
                    CountryId = item.CountryId is Guid picked && picked != Guid.Empty ? picked : null,
                    IsDeleted = false,
                    Status = 0
                };
                db.Set<RequestItem>().Add(requestItem);

                if (!recipesByItem.TryGetValue(item.ItemId, out var recipe)) continue;
                var people = await PeopleAsync(item.PeopleCount);
                foreach (var ingredient in recipe)
                {
                    db.Set<RequestContent>().Add(new RequestContent
                    {
                        Id = Guid.NewGuid(),
                        RequestItemId = requestItem.Id,
                        ContentId = ingredient.ContentId,
                        Quantity = ingredient.Quantity * repeat * people,
                        IsDeleted = false,
                        Status = 0
                    });
                }
            }

            foreach (var line in lines)
            {
                db.Set<RequestShoppingLine>().Add(new RequestShoppingLine
                {
                    Id = Guid.NewGuid(),
                    RequestId = order.Id,
                    ContentId = line.ContentId,
                    PurchasePlaceId = line.PurchasePlaceId,
                    UnitId = line.UnitId,
                    Quantity = line.Quantity,
                    IsDeleted = false,
                    Status = 0
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            return new Result<Guid>(HttpStatusCode.OK, order.Id, null);
        }

        private static async Task MarkChildrenDeleted(IFoodsDbContext db, Guid requestId, CancellationToken cancellationToken)
        {
            var items = await db.Set<RequestItem>().Where(e => e.RequestId == requestId && !e.IsDeleted).ToListAsync(cancellationToken);
            var itemIds = items.Select(e => e.Id).ToList();
            var contents = await db.Set<RequestContent>().Where(e => itemIds.Contains(e.RequestItemId) && !e.IsDeleted).ToListAsync(cancellationToken);
            var lines = await db.Set<RequestShoppingLine>().Where(e => e.RequestId == requestId && !e.IsDeleted).ToListAsync(cancellationToken);
            foreach (var item in items) item.IsDeleted = true;
            foreach (var content in contents) content.IsDeleted = true;
            foreach (var line in lines) line.IsDeleted = true;
        }
    }

    public sealed class ReorderCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<ReorderCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(ReorderCommand request, CancellationToken cancellationToken)
        {
            var source = await db.Set<Request>().FirstOrDefaultAsync(e => e.Id == request.Id && e.UserId == currentUser.UserId && !e.IsDeleted, cancellationToken);
            if (source == null)
                return new Result<Guid>(HttpStatusCode.NotFound, Guid.Empty, new List<string> { "الطلب غير موجود" });

            var copy = new Request
            {
                Id = Guid.NewGuid(),
                UserId = currentUser.UserId,
                Name = await OrderNaming.NextAsync(db, currentUser.UserId, DateTime.UtcNow.Date, cancellationToken),
                Date = DateTime.UtcNow.Date,
                Notes = source.Notes,
                Code = DateTime.UtcNow.ToString("yyyyMMdd"),
                CreatedAt = DateTime.Now,
                IsDeleted = false,
                Status = OrderStatuses.Confirmed
            };
            db.Set<Request>().Add(copy);
            source.ReorderedAt = DateTime.Now;

            var items = await db.Set<RequestItem>().Where(e => e.RequestId == source.Id && !e.IsDeleted).ToListAsync(cancellationToken);
            foreach (var item in items)
            {
                var copyItem = new RequestItem
                {
                    Id = Guid.NewGuid(),
                    RequestId = copy.Id,
                    ItemId = item.ItemId,
                    Quantity = item.Quantity,
                    RepeatCount = item.RepeatCount,
                    PeopleCount = item.PeopleCount,
                    CountryId = item.CountryId,
                    IsDeleted = false,
                    Status = 0
                };
                db.Set<RequestItem>().Add(copyItem);
                var contents = await db.Set<RequestContent>().Where(e => e.RequestItemId == item.Id && !e.IsDeleted).ToListAsync(cancellationToken);
                foreach (var content in contents)
                {
                    db.Set<RequestContent>().Add(new RequestContent
                    {
                        Id = Guid.NewGuid(),
                        RequestItemId = copyItem.Id,
                        ContentId = content.ContentId,
                        Quantity = content.Quantity,
                        IsDeleted = false,
                        Status = 0
                    });
                }
            }

            var lines = await db.Set<RequestShoppingLine>().Where(e => e.RequestId == source.Id && !e.IsDeleted).ToListAsync(cancellationToken);
            foreach (var line in lines)
            {
                db.Set<RequestShoppingLine>().Add(new RequestShoppingLine
                {
                    Id = Guid.NewGuid(),
                    RequestId = copy.Id,
                    ContentId = line.ContentId,
                    PurchasePlaceId = line.PurchasePlaceId,
                    UnitId = line.UnitId,
                    Quantity = line.Quantity,
                    IsDeleted = false,
                    Status = 0
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            return new Result<Guid>(HttpStatusCode.OK, copy.Id, null);
        }
    }

    /// <summary>Lets the user give an order their own name (keeps its items and status).</summary>
    public sealed class RenameOrderCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<RenameOrderCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(RenameOrderCommand request, CancellationToken cancellationToken)
        {
            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return new Result<Guid>(HttpStatusCode.BadRequest, Guid.Empty, new List<string> { "اكتب اسم الطلب" });
            if (name.Length > 150)
                name = name[..150];

            var order = await db.Set<Request>().FirstOrDefaultAsync(e => e.Id == request.Id && e.UserId == currentUser.UserId && !e.IsDeleted, cancellationToken);
            if (order == null)
                return new Result<Guid>(HttpStatusCode.NotFound, Guid.Empty, new List<string> { "الطلب غير موجود" });

            order.Name = name;
            await db.SaveChangesAsync(cancellationToken);
            return new Result<Guid>(HttpStatusCode.OK, order.Id, null);
        }
    }
}
