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

    /// <summary>History state of a confirmed order.</summary>
    public static class OrderHistoryStates
    {
        /// <summary>إنشاء بدون طباعة</summary>
        public const string Created = "created";
        /// <summary>تم الطباعة</summary>
        public const string Printed = "printed";
        /// <summary>أعيد طلبه</summary>
        public const string Reordered = "reordered";
        /// <summary>تمت المشاركة</summary>
        public const string Shared = "shared";
        /// <summary>أعيدت الطباعة</summary>
        public const string Reprinted = "reprinted";
        /// <summary>أعيدت المشاركة</summary>
        public const string Reshared = "reshared";
    }

    /// <summary>
    /// GET customer/orders/history — confirmed orders, newest first.
    /// Text searches the order name, code and dish names; Dish searches dish names only.
    /// </summary>
    public sealed record OrderHistoryQuery : IQuery<OrderHistoryPage>
    {
        public int Skip { get; init; }
        public int Take { get; init; } = 20;
        public string? Text { get; init; }
        public string? State { get; init; }
        public DateTime? From { get; init; }
        public DateTime? To { get; init; }
        public string? Dish { get; init; }
        /// <summary>Order name (or code) only.</summary>
        public string? Name { get; init; }
    }

    public sealed record OrderHistoryDish(Guid ItemId, string? Name, string? ImagePath, decimal RepeatCount, string? Summary = null);
    public sealed record OrderHistoryItem(
        Guid Id, string? Name, string? Code, DateTime Date, DateTime? CreatedAt,
        string State, DateTime? PrintedAt, DateTime? ReorderedAt, DateTime? SharedAt,
        int DishCount, int LineCount, List<OrderHistoryDish> Dishes, decimal? Total = null);
    public sealed record OrderHistoryCounts(int All, int Created, int Printed, int Reordered, int Shared, int Reprinted = 0, int Reshared = 0);
    public sealed record OrderHistoryPage(List<OrderHistoryItem> Items, int Total, bool HasMore, OrderHistoryCounts Counts);

    public sealed record MarkOrderPrintedCommand(Guid Id) : ICommand<Guid>;
    public sealed record MarkOrderReorderedCommand(Guid Id) : ICommand<Guid>;
    public sealed record MarkOrderSharedCommand(Guid Id) : ICommand<Guid>;

    public sealed class OrderHistoryQueryHandler(IFoodsDbContext db, ICurrentUser currentUser) : IQueryHandler<OrderHistoryQuery, OrderHistoryPage>
    {
        public async Task<Result<OrderHistoryPage>> Handle(OrderHistoryQuery request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId;
            var take = request.Take <= 0 ? 20 : Math.Min(request.Take, 100);
            var skip = Math.Max(0, request.Skip);

            var orders = db.Set<Request>().Where(e => e.UserId == userId && !e.IsDeleted && e.Status == OrderStatuses.Confirmed);

            var text = request.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                var dishIds = db.Items.Where(i => i.Name != null && i.Name.Contains(text)).Select(i => i.Id);
                orders = orders.Where(e =>
                    (e.Name != null && e.Name.Contains(text)) ||
                    (e.Code != null && e.Code.Contains(text)) ||
                    db.Set<RequestItem>().Any(ri => ri.RequestId == e.Id && !ri.IsDeleted && dishIds.Contains(ri.ItemId)));
            }

            var dish = request.Dish?.Trim();
            if (!string.IsNullOrEmpty(dish))
            {
                var dishIds = db.Items.Where(i => i.Name != null && i.Name.Contains(dish)).Select(i => i.Id);
                orders = orders.Where(e => db.Set<RequestItem>().Any(ri => ri.RequestId == e.Id && !ri.IsDeleted && dishIds.Contains(ri.ItemId)));
            }

            var name = request.Name?.Trim();
            if (!string.IsNullOrEmpty(name))
                orders = orders.Where(e => (e.Name != null && e.Name.Contains(name)) || (e.Code != null && e.Code.Contains(name)));

            if (request.From is DateTime from)
                orders = orders.Where(e => e.Date >= from.Date);
            if (request.To is DateTime to)
            {
                var end = to.Date.AddDays(1);
                orders = orders.Where(e => e.Date < end);
            }

            // The state is the latest thing done with the order (print, share or re-order).
            System.Linq.Expressions.Expression<Func<Request, bool>> created = e => e.PrintedAt == null && e.SharedAt == null && e.ReorderedAt == null;
            // Printed / shared once vs. again (PrintCount / ShareCount > 1).
            System.Linq.Expressions.Expression<Func<Request, bool>> printed = e => e.PrintedAt != null && e.PrintCount <= 1
                && (e.SharedAt == null || e.PrintedAt > e.SharedAt) && (e.ReorderedAt == null || e.PrintedAt > e.ReorderedAt);
            System.Linq.Expressions.Expression<Func<Request, bool>> reprinted = e => e.PrintedAt != null && e.PrintCount > 1
                && (e.SharedAt == null || e.PrintedAt > e.SharedAt) && (e.ReorderedAt == null || e.PrintedAt > e.ReorderedAt);
            System.Linq.Expressions.Expression<Func<Request, bool>> shared = e => e.SharedAt != null && e.ShareCount <= 1
                && (e.PrintedAt == null || e.SharedAt >= e.PrintedAt) && (e.ReorderedAt == null || e.SharedAt > e.ReorderedAt);
            System.Linq.Expressions.Expression<Func<Request, bool>> reshared = e => e.SharedAt != null && e.ShareCount > 1
                && (e.PrintedAt == null || e.SharedAt >= e.PrintedAt) && (e.ReorderedAt == null || e.SharedAt > e.ReorderedAt);
            System.Linq.Expressions.Expression<Func<Request, bool>> reordered = e => e.ReorderedAt != null
                && (e.PrintedAt == null || e.ReorderedAt >= e.PrintedAt) && (e.SharedAt == null || e.ReorderedAt >= e.SharedAt);

            var counts = new OrderHistoryCounts(
                await orders.CountAsync(cancellationToken),
                await orders.CountAsync(created, cancellationToken),
                await orders.CountAsync(printed, cancellationToken),
                await orders.CountAsync(reordered, cancellationToken),
                await orders.CountAsync(shared, cancellationToken),
                await orders.CountAsync(reprinted, cancellationToken),
                await orders.CountAsync(reshared, cancellationToken));

            switch (request.State)
            {
                case OrderHistoryStates.Created: orders = orders.Where(created); break;
                case OrderHistoryStates.Printed: orders = orders.Where(printed); break;
                case OrderHistoryStates.Reordered: orders = orders.Where(reordered); break;
                case OrderHistoryStates.Shared: orders = orders.Where(shared); break;
                case OrderHistoryStates.Reprinted: orders = orders.Where(reprinted); break;
                case OrderHistoryStates.Reshared: orders = orders.Where(reshared); break;
            }

            var total = await orders.CountAsync(cancellationToken);
            var page = await orders
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.Code)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            var ids = page.Select(e => e.Id).ToList();
            var requestItems = await db.Set<RequestItem>().Where(e => ids.Contains(e.RequestId) && !e.IsDeleted).ToListAsync(cancellationToken);
            var itemIds = requestItems.Select(e => e.ItemId).Distinct().ToList();
            var menuItems = await db.Items.Where(e => itemIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, cancellationToken);
            var book = await Foods.Application.Customer.Pricing.PriceBook.For(db, currentUser.UserId, cancellationToken);
            var totals = new Dictionary<Guid, decimal>();
            if (book != null)
            {
                var shopping = await db.Set<RequestShoppingLine>()
                    .Where(e => ids.Contains(e.RequestId) && !e.IsDeleted && e.Quantity > 0)
                    .ToListAsync(cancellationToken);
                var lineContentIds = shopping.Select(e => e.ContentId).Distinct().ToList();
                await book.Load(lineContentIds, cancellationToken);
                var modes = await db.Contents.Where(e => lineContentIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, e => e.PurchaseMode, cancellationToken);
                foreach (var line in shopping)
                {
                    var cost = modes.GetValueOrDefault(line.ContentId) == PurchaseRules.Money
                        ? line.Quantity
                        : book.Cost(line.ContentId, line.UnitId, line.Quantity);
                    totals[line.RequestId] = totals.GetValueOrDefault(line.RequestId) + (cost ?? 0);
                }
            }
            var lineCounts = await db.Set<RequestShoppingLine>()
                .Where(e => ids.Contains(e.RequestId) && !e.IsDeleted && e.Quantity > 0)
                .GroupBy(e => e.RequestId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(e => e.Key, e => e.Count, cancellationToken);

            var items = page.Select(order =>
            {
                var dishes = requestItems
                    .Where(e => e.RequestId == order.Id)
                    .GroupBy(e => e.ItemId)
                    .Select(g =>
                    {
                        var row = g.First();
                        menuItems.TryGetValue(row.ItemId, out var menuItem);
                        return new OrderHistoryDish(row.ItemId, menuItem?.Name, menuItem?.ImagePath, row.RepeatCount == 0 ? row.Quantity : row.RepeatCount, menuItem?.Summary ?? menuItem?.Description);
                    }).ToList();
                var latest = new[]
                {
                    (At: order.ReorderedAt, State: OrderHistoryStates.Reordered),
                    (At: order.SharedAt, State: OrderHistoryStates.Shared),
                    (At: order.PrintedAt, State: OrderHistoryStates.Printed)
                }.Where(e => e.At != null).OrderByDescending(e => e.At).FirstOrDefault();
                var state = latest.State ?? OrderHistoryStates.Created;
                if (state == OrderHistoryStates.Printed && order.PrintCount > 1) state = OrderHistoryStates.Reprinted;
                if (state == OrderHistoryStates.Shared && order.ShareCount > 1) state = OrderHistoryStates.Reshared;
                return new OrderHistoryItem(order.Id, order.Name, order.Code, order.Date, order.CreatedAt, state,
                    order.PrintedAt, order.ReorderedAt, order.SharedAt, dishes.Count, lineCounts.GetValueOrDefault(order.Id), dishes,
                    book == null ? null : Foods.Application.Customer.Pricing.PriceBook.Round(totals.GetValueOrDefault(order.Id)));
            }).ToList();

            return new Result<OrderHistoryPage>(HttpStatusCode.OK,
                new OrderHistoryPage(items, total, skip + items.Count < total, counts), null);
        }
    }

    public sealed class MarkOrderPrintedCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<MarkOrderPrintedCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(MarkOrderPrintedCommand request, CancellationToken cancellationToken)
        {
            var order = await db.Set<Request>().FirstOrDefaultAsync(e => e.Id == request.Id && e.UserId == currentUser.UserId && !e.IsDeleted, cancellationToken);
            if (order == null)
                return new Result<Guid>(HttpStatusCode.NotFound, Guid.Empty, new List<string> { "الطلب غير موجود" });

            // Orders printed before the counter existed count as printed once.
            order.PrintCount = Math.Max(order.PrintCount, order.PrintedAt != null ? 1 : 0) + 1;
            order.PrintedAt = DateTime.Now;
            await db.SaveChangesAsync(cancellationToken);
            return new Result<Guid>(HttpStatusCode.OK, order.Id, null);
        }
    }

    public sealed class MarkOrderReorderedCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<MarkOrderReorderedCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(MarkOrderReorderedCommand request, CancellationToken cancellationToken)
        {
            var order = await db.Set<Request>().FirstOrDefaultAsync(e => e.Id == request.Id && e.UserId == currentUser.UserId && !e.IsDeleted, cancellationToken);
            if (order == null)
                return new Result<Guid>(HttpStatusCode.NotFound, Guid.Empty, new List<string> { "الطلب غير موجود" });

            order.ReorderedAt = DateTime.Now;
            await db.SaveChangesAsync(cancellationToken);
            return new Result<Guid>(HttpStatusCode.OK, order.Id, null);
        }
    }

    public sealed class MarkOrderSharedCommandHandler(IFoodsDbContext db, ICurrentUser currentUser) : ICommandHandler<MarkOrderSharedCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(MarkOrderSharedCommand request, CancellationToken cancellationToken)
        {
            var order = await db.Set<Request>().FirstOrDefaultAsync(e => e.Id == request.Id && e.UserId == currentUser.UserId && !e.IsDeleted, cancellationToken);
            if (order == null)
                return new Result<Guid>(HttpStatusCode.NotFound, Guid.Empty, new List<string> { "الطلب غير موجود" });

            order.ShareCount = Math.Max(order.ShareCount, order.SharedAt != null ? 1 : 0) + 1;
            order.SharedAt = DateTime.Now;
            await db.SaveChangesAsync(cancellationToken);
            return new Result<Guid>(HttpStatusCode.OK, order.Id, null);
        }
    }
}
