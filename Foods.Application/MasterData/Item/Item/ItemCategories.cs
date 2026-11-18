namespace Foods.Application.MasterData.Item.Item
{
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData.Item;

    /// <summary>A dish's other categories (its main one is Item.ItemCategoryId).</summary>
    public static class ItemCategories
    {
        /// <summary>Replaces the extra categories of a dish (null = leave them as they are). Saved with the caller's SaveChanges.</summary>
        public static async Task Save(IFoodsDbContext db, Guid itemId, Guid mainCategoryId, List<Guid>? categoryIds, CancellationToken ct)
        {
            if (categoryIds == null) return;
            var wanted = categoryIds.Where(e => e != Guid.Empty && e != mainCategoryId).Distinct().ToList();
            var rows = await db.ItemExtraCategories.Where(e => e.ItemId == itemId && !e.IsDeleted).ToListAsync(ct);
            foreach (var row in rows.Where(e => !wanted.Contains(e.ItemCategoryId)))
                row.IsDeleted = true;
            foreach (var id in wanted.Where(id => rows.All(e => e.ItemCategoryId != id)))
                db.ItemExtraCategories.Add(new ItemExtraCategory { Id = Guid.NewGuid(), ItemId = itemId, ItemCategoryId = id });
        }

        public static async Task<Dictionary<Guid, List<Guid>>> Of(IFoodsDbContext db, IReadOnlyCollection<Guid> itemIds, CancellationToken ct)
        {
            var rows = await db.ItemExtraCategories
                .Where(e => itemIds.Contains(e.ItemId) && !e.IsDeleted)
                .Select(e => new { e.ItemId, e.ItemCategoryId })
                .ToListAsync(ct);
            return rows.GroupBy(e => e.ItemId).ToDictionary(g => g.Key, g => g.Select(e => e.ItemCategoryId).ToList());
        }
    }
}
