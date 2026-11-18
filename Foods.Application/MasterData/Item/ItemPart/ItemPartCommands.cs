namespace Foods.Application.MasterData.Item.ItemPart
{
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData.Item;
    using Foods.Application.Abstraction.Query;
    using Foods.Application.Abstraction.Command;

    /// <summary>One part (صنف) of a dish as the admin sees it.</summary>
    public sealed record ItemPartRow(Guid Id, Guid PartItemId, string? Name, string? ImagePath, bool ShowToCustomer, bool IsOptional, bool IsDefault, int Sort);
    public sealed record ItemPartInput(Guid PartItemId, bool IsOptional, bool IsDefault);

    public sealed record GetItemPartsQuery(Guid ItemId) : IQuery<List<ItemPartRow>>;
    public sealed record SaveItemPartsCommand(Guid ItemId, IReadOnlyList<ItemPartInput>? Parts) : ICommand<List<ItemPartRow>>;

    public static class ItemParts
    {
        /// <summary>How deep parts of parts are followed (also stops loops).</summary>
        public const int MaxDepth = 4;

        public static async Task<List<ItemPartRow>> Rows(IFoodsDbContext db, Guid itemId, CancellationToken ct)
        {
            var parts = await db.ItemParts.Where(e => e.ItemId == itemId && !e.IsDeleted && e.CountryId == null).OrderBy(e => e.Sort).ToListAsync(ct);
            var ids = parts.Select(e => e.PartItemId).Distinct().ToList();
            var items = await db.Items.Where(e => ids.Contains(e.Id) && !e.IsDeleted).ToDictionaryAsync(e => e.Id, ct);
            return parts.Where(e => items.ContainsKey(e.PartItemId)).Select(e =>
            {
                var item = items[e.PartItemId];
                return new ItemPartRow(e.Id, e.PartItemId, item.Name, item.ImagePath, item.ShowToCustomer, e.IsOptional, e.IsDefault, e.Sort);
            }).ToList();
        }

        /// <summary>True when <paramref name="partId"/> (or any of its parts) is <paramref name="itemId"/>.</summary>
        public static async Task<bool> MakesLoop(IFoodsDbContext db, Guid itemId, Guid partId, CancellationToken ct)
        {
            var seen = new HashSet<Guid>();
            var level = new List<Guid> { partId };
            for (var depth = 0; depth <= MaxDepth * 4 && level.Count > 0; depth++)
            {
                if (level.Contains(itemId)) return true;
                foreach (var id in level) seen.Add(id);
                var next = await db.ItemParts.Where(e => level.Contains(e.ItemId) && !e.IsDeleted).Select(e => e.PartItemId).Distinct().ToListAsync(ct);
                level = next.Where(e => !seen.Contains(e)).ToList();
            }
            return false;
        }
    }

    public sealed class GetItemPartsQueryHandler(IFoodsDbContext db) : IQueryHandler<GetItemPartsQuery, List<ItemPartRow>>
    {
        public async Task<Result<List<ItemPartRow>>> Handle(GetItemPartsQuery request, CancellationToken cancellationToken)
            => new(HttpStatusCode.OK, await ItemParts.Rows(db, request.ItemId, cancellationToken), null);
    }

    /// <summary>Replaces all parts of a dish with the given list (order = sort).</summary>
    public sealed class SaveItemPartsCommandHandler(IFoodsDbContext db) : ICommandHandler<SaveItemPartsCommand, List<ItemPartRow>>
    {
        public async Task<Result<List<ItemPartRow>>> Handle(SaveItemPartsCommand request, CancellationToken cancellationToken)
        {
            if (!await db.Items.AnyAsync(e => e.Id == request.ItemId && !e.IsDeleted, cancellationToken))
                return Bad("الأكلة غير موجودة", HttpStatusCode.NotFound);

            var wanted = (request.Parts ?? Array.Empty<ItemPartInput>()).Where(e => e.PartItemId != Guid.Empty).ToList();
            if (wanted.Count > 40)
                return Bad("عدد الأصناف كبير قوي");
            if (wanted.Any(e => e.PartItemId == request.ItemId))
                return Bad("الأكلة متتضافش كصنف لنفسها");
            if (wanted.Select(e => e.PartItemId).Distinct().Count() != wanted.Count)
                return Bad("الصنف متكرر");
            var ids = wanted.Select(e => e.PartItemId).ToList();
            var found = await db.Items.CountAsync(e => ids.Contains(e.Id) && !e.IsDeleted, cancellationToken);
            if (found != ids.Count)
                return Bad("اختار من الأكلات الموجودة بس");
            foreach (var part in wanted)
            {
                if (await ItemParts.MakesLoop(db, request.ItemId, part.PartItemId, cancellationToken))
                {
                    var name = await db.Items.Where(e => e.Id == part.PartItemId).Select(e => e.Name).FirstOrDefaultAsync(cancellationToken);
                    return Bad($"\"{name}\" فيها الأكلة دي كصنف، فمينفعش تبقى صنف فيها");
                }
            }

            var old = await db.ItemParts.Where(e => e.ItemId == request.ItemId && !e.IsDeleted && e.CountryId == null).ToListAsync(cancellationToken);
            foreach (var row in old) row.IsDeleted = true;
            for (var i = 0; i < wanted.Count; i++)
            {
                db.ItemParts.Add(new Foods.Domain.Entity.MasterData.Item.ItemPart
                {
                    Id = Guid.NewGuid(),
                    ItemId = request.ItemId,
                    PartItemId = wanted[i].PartItemId,
                    IsOptional = wanted[i].IsOptional,
                    IsDefault = !wanted[i].IsOptional || wanted[i].IsDefault,
                    Sort = i + 1,
                    IsDeleted = false,
                    Status = 0
                });
            }
            await db.SaveChangesAsync(cancellationToken);
            return new(HttpStatusCode.OK, await ItemParts.Rows(db, request.ItemId, cancellationToken), null);
        }

        private static Result<List<ItemPartRow>> Bad(string message, HttpStatusCode code = HttpStatusCode.BadRequest)
            => new(code, null, new List<string> { message });
    }
}
