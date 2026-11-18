namespace Foods.API.Seed;

using Foods.Domain.Entity.MasterData;
using Foods.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

public static class CuisineSeeder
{
    static readonly Guid Sharqi = Guid.Parse("a1111111-1111-1111-1111-111111111111");
    static readonly Guid Gharbi = Guid.Parse("a2222222-2222-2222-2222-222222222222");
    static readonly Guid Mashwayat = Guid.Parse("a3333333-3333-3333-3333-333333333333");
    static readonly Guid Sehi = Guid.Parse("a4444444-4444-4444-4444-444444444444");
    static readonly Guid Nabati = Guid.Parse("a5555555-5555-5555-5555-555555555555");

    public static async Task EnsureAsync(FoodsDbContext db)
    {
        var seed = new[]
        {
            new Cuisine { Id = Sharqi, Name = "شرقي", IconPath = "cloche", Color = "#FFEAC5", TextColor = "#3A2210", Sort = 1, IsDeleted = false, Status = 0 },
            new Cuisine { Id = Gharbi, Name = "غربي", IconPath = "chef", Color = "#E2F1FB", TextColor = "#1E3A5F", Sort = 2, IsDeleted = false, Status = 0 },
            new Cuisine { Id = Mashwayat, Name = "مشويات", IconPath = "grill", Color = "#FFE8D6", TextColor = "#4A2A12", Sort = 3, IsDeleted = false, Status = 0 },
            new Cuisine { Id = Sehi, Name = "صحي", IconPath = "leaf", Color = "#E4F2C6", TextColor = "#1E2A1E", Sort = 4, IsDeleted = false, Status = 0 },
            new Cuisine { Id = Nabati, Name = "نباتي", IconPath = "vegan", Color = "#EEF8E9", TextColor = "#1E3A28", Sort = 5, IsDeleted = false, Status = 0 }
        };

        foreach (var cuisine in seed)
        {
            var existing = await db.Cuisines.FirstOrDefaultAsync(e => e.Id == cuisine.Id);
            if (existing == null)
                db.Cuisines.Add(cuisine);
            else if (existing.IsDeleted || string.IsNullOrWhiteSpace(existing.Name))
            {
                existing.Name = cuisine.Name;
                existing.IconPath = cuisine.IconPath;
                existing.Color = cuisine.Color;
                existing.TextColor = cuisine.TextColor;
                existing.Sort = cuisine.Sort;
                existing.IsDeleted = false;
            }
        }

        await db.SaveChangesAsync();

        var items = await db.Items.Where(e => !e.IsDeleted && e.CuisineId == null).ToListAsync();
        if (items.Count == 0)
            return;

        foreach (var item in items)
            item.CuisineId = GuessCuisine(item.Name);

        await db.SaveChangesAsync();
    }

    static Guid GuessCuisine(string? name)
    {
        var value = name ?? "";
        if (ContainsAny(value, "مشوي", "مشاوي", "كباب", "شيش", "ستيك", "باربكيو", "شواء"))
            return Mashwayat;
        if (ContainsAny(value, "نباتي", "نبات", "فيجان", "خضار", "فول", "حمص", "فلافل", "طعمية", "مجدرة", "عدس"))
            return Nabati;
        if (ContainsAny(value, "صحي", "سلطة", "دايت", "خفيف", "زبادي", "شوفان", "جرين"))
            return Sehi;
        if (ContainsAny(value, "باستا", "بيتزا", "برجر", "لازانيا", "مكرونة", "معكرونة", "فريتش", "غربي", "ستيك", "نودلز", "سوشي"))
            return Gharbi;
        return Sharqi;
    }

    static bool ContainsAny(string value, params string[] parts) =>
        parts.Any(part => value.Contains(part, StringComparison.OrdinalIgnoreCase));
}
