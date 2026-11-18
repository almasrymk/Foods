using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Foods.Domain.Entity.MasterData;
using Foods.Domain.Entity.MasterData.Content;
using Foods.Domain.Entity.MasterData.Item;
using Foods.Domain.Entity.Security;
using Foods.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Foods.API.Seed;

public sealed class CatalogSeeder
{
    private readonly FoodsDbContext db;
    private readonly string uploads;
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(8) };

    public CatalogSeeder(FoodsDbContext db, string webRoot)
    {
        this.db = db;
        uploads = Path.Combine(webRoot, "uploads");
        http.DefaultRequestHeaders.UserAgent.ParseAdd("FoodsSeeder/1.0 (local recipe catalog)");
    }

    public async Task RunAsync()
    {
        Directory.CreateDirectory(uploads);
        foreach (var file in Directory.GetFiles(uploads, "*.png")) File.Delete(file);

        foreach (var table in new[]
        {
            "RequestShoppingLines", "RequestContents", "RequestItems", "RequestTracks", "Requests",
            "Favorites", "FamilyMembers",
            "ItemVideos", "ItemSteps", "ItemContents", "ItemMeals", "ItemLevels", "ItemCountries",
            "ContentPrices", "ContentLevels", "ContentTradeMarks", "ContentAlternatives",
            "Items", "Contents", "Meals", "PurchasePlaces", "Units",
            "ItemCategories", "ItemGroups", "ContentCategories", "ContentGroup",
            "Levels", "TradeMarks", "TradeMarkContentGroups", "Users", "Countries"
        })
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM [" + table + "]");
        }

        var admin = User(RoleIds.Admin, "Admin", "Admin1234", "المدير");
        var customer = User(RoleIds.Customer, "Customer", "Customer1234", "العميل");
        customer.CountryId = CountryIds.Egypt;
        db.Set<User>().AddRange(admin, customer);

        var places = new Dictionary<string, PurchasePlace>();
        foreach (var name in new[] { "البقالة", "الخضارجي", "الجزار", "سوق الفراخ", "سوق السمك", "الفرن" })
            places[name] = Add(new PurchasePlace { Name = name });

        var units = new Dictionary<string, Unit>();
        units["جرام"] = Add(new Unit { Name = "جرام", NickName = "جم", BasicQuantity = 1, BeDivided = true });
        units["حبة"] = Add(new Unit { Name = "حبة", NickName = "حبة", BasicQuantity = 1, BeDivided = false });
        units["مل"] = Add(new Unit { Name = "ملليلتر", NickName = "مل", BasicQuantity = 1, BeDivided = true });
        units["كوب"] = Add(new Unit { Name = "كوب", NickName = "كوب", BasicQuantity = 240, BeDivided = true });
        units["ملعقة"] = Add(new Unit { Name = "ملعقة كبيرة", NickName = "م.ك", BasicQuantity = 15, BeDivided = true });
        units["ربطة"] = Add(new Unit { Name = "ربطة", NickName = "ربطة", BasicQuantity = 1, BeDivided = false });

        var contentCategories = Map(new[] { "خضار", "بقول", "لحوم", "ألبان", "بهارات", "مخبوزات", "حبوب" }, name => new ContentCategory { Name = name });
        var contentGroups = Map(new[] { "طازج", "جاف", "معلب" }, name => new ContentGroup { Name = name });
        var itemCategories = Map(new[] { "مصري", "شامي", "خليجي", "مغاربي" }, name => new ItemCategory { Name = name });
        var itemGroups = Map(new[] { "أطباق رئيسية", "مشويات", "شوربات", "مخبوزات", "سلطات", "حلويات", "مشروبات", "فطور شعبي" }, name => new ItemGroup { Name = name });

        var pantry = Pantry();
        var contents = new Dictionary<string, Content>();
        foreach (var row in pantry)
        {
            var photo = await Picture(row.Key, row.Search);
            contents[row.Key] = Add(new Content
            {
                Name = row.Name,
                ContentCategoryId = contentCategories[row.Category].Id,
                ContentGroupId = contentGroups[row.Group].Id,
                UnitId = units[row.Unit].Id,
                MiniQuantity = row.Mini,
                PurchasePlaceId = places[row.Place].Id,
                ImagePath = photo
            });
        }

        var meals = new Dictionary<string, Meal>
        {
            ["فطار"] = Add(new Meal { Name = "فطار", Description = "وجبة الصباح: فول ومخبوزات وبيض وألبان خفيفة من المشرق والمغرب.", ImagePath = await Picture("meal-breakfast", "egyptian breakfast foul falafel") }),
            ["غداء"] = Add(new Meal { Name = "غداء", Description = "وجبة الغداء: أطباق الرز واللحوم والخضار المطهوة على مائدة شرقية.", ImagePath = await Picture("meal-lunch", "middle eastern lunch rice meat") }),
            ["عشاء"] = Add(new Meal { Name = "عشاء", Description = "وجبة العشاء: مشويات وشوربات وأطباق أخف تُقدَّم في آخر اليوم.", ImagePath = await Picture("meal-dinner", "grilled middle eastern dinner") }),
            ["حلويات"] = Add(new Meal { Name = "حلويات", Description = "حلويات شرقية: كنافة وقطايف وبقلاوة وبسبوسة ومهلبية.", ImagePath = await Picture("meal-dessert", "middle eastern dessert knafeh baklava") }),
            ["مشروبات"] = Add(new Meal { Name = "مشروبات", Description = "مشروبات شرقية: شاي وقهوة وكركديه وعصائر وتمر هندي.", ImagePath = await Picture("meal-drinks", "arabic tea coffee karak") })
        };

        var countries = SeedCountries();
        var photos = IndexPhotos();
        var dishes = Dishes();
        foreach (var mealName in MealNames)
            if (dishes.Count(d => d.Meal == mealName) != 1000)
                throw new InvalidOperationException("كل وجبة لازم يبقى تحتها 1000 صنف.");

        var batch = 0;
        var made = new Dictionary<string, Item>();
        var madeMeals = new HashSet<string>();
        foreach (var dish in dishes)
        {
            var home = CountriesFor(dish.Name, dish.Region);
            if (!made.TryGetValue(dish.Name, out var item))
            {
            item = Add(new Item
            {
                Name = dish.Name,
                ItemCategoryId = itemCategories[dish.Region].Id,
                ItemGroupId = itemGroups[Group(dish.Name)].Id,
                Summary = dish.Name + " من المطبخ " + dish.Region + ". البلد الرئيسية: " + home.Primary + ".",
                Description = Method(dish.Name, dish.Meal),
                ImagePath = PhotoFor(photos, dish.Meal, dish.Name)
            });
            made[dish.Name] = item;
            LinkCountries(item, home, countries);
            var parts = Steps(dish.Name, dish.Meal);
            for (var i = 0; i < parts.Length; i++)
                Add(new ItemStep { ItemId = item.Id, Sort = i + 1, Body = parts[i] });
            Add(new ItemVideo
            {
                ItemId = item.Id,
                Sort = 1,
                Title = "طريقة عمل " + dish.Name,
                Url = "https://www.youtube.com/results?search_query=" + Uri.EscapeDataString("طريقة عمل " + dish.Name)
            });
            foreach (var (key, qty) in Recipe(dish.Name))
            {
                var content = contents[key];
                Add(new ItemContent
                {
                    ItemId = item.Id,
                    ContentId = content.Id,
                    UnitId = content.UnitId,
                    Quantity = qty
                });
            }
            }
            if (madeMeals.Add(item.Id + ":" + dish.Meal))
                Add(new ItemMeal { ItemId = item.Id, MealId = meals[dish.Meal].Id });
            if (++batch % 25 == 0) await db.SaveChangesAsync();
        }

        await db.SaveChangesAsync();
        Console.WriteLine("SEED DONE " + dishes.Count);
    }

    public async Task FillMenuAsync()
    {
        Directory.CreateDirectory(uploads);
        var countries = await EnsureCountries();
        var meals = await EnsureMealsAsync();
        await EnsureGroupAsync("مشروبات");
        await EnsurePantryAsync();
        var categories = await db.ItemCategories.Where(e => !e.IsDeleted).ToDictionaryAsync(e => e.Name ?? "");
        var groups = await db.ItemGroups.Where(e => !e.IsDeleted).ToDictionaryAsync(e => e.Name ?? "");
        var contentByName = await db.Contents.Where(e => !e.IsDeleted).ToDictionaryAsync(e => e.Name ?? "");
        var keyed = new Dictionary<string, Content>();
        foreach (var row in Pantry())
            if (contentByName.TryGetValue(row.Name, out var content))
                keyed[row.Key] = content;

        var byName = new Dictionary<string, Item>();
        foreach (var item in await db.Items.Where(e => !e.IsDeleted).ToListAsync())
            if (!string.IsNullOrEmpty(item.Name))
                byName.TryAdd(CleanDishName(item.Name), item);

        var linkedIds = (await db.ItemCountries.Where(e => !e.IsDeleted).Select(e => e.ItemId).Distinct().ToListAsync()).ToHashSet();
        var mealLinks = (await db.ItemMeals.Where(e => !e.IsDeleted).Select(e => new { e.ItemId, e.MealId }).ToListAsync())
            .Select(e => (e.ItemId, e.MealId)).ToHashSet();
        var photos = IndexPhotos();
        var added = 0;
        var batch = 0;
        foreach (var dish in Dishes())
        {
            if (!meals.TryGetValue(dish.Meal, out var meal) || !categories.TryGetValue(dish.Region, out var category) || !groups.TryGetValue(Group(dish.Name), out var group))
                continue;

            var places = CountriesFor(dish.Name, dish.Region);
            if (!byName.TryGetValue(dish.Name, out var item))
            {
                item = Add(new Item
                {
                    Name = dish.Name,
                    ItemCategoryId = category.Id,
                    ItemGroupId = group.Id,
                    Summary = dish.Name + " من المطبخ " + dish.Region + ". البلد الرئيسية: " + places.Primary + ".",
                    Description = Method(dish.Name, dish.Meal),
                    ImagePath = PhotoFor(photos, dish.Meal, dish.Name)
                });
                byName[dish.Name] = item;
                var parts = Steps(dish.Name, dish.Meal);
                for (var i = 0; i < parts.Length; i++)
                    Add(new ItemStep { ItemId = item.Id, Sort = i + 1, Body = parts[i] });
                Add(new ItemVideo
                {
                    ItemId = item.Id,
                    Sort = 1,
                    Title = "طريقة عمل " + dish.Name,
                    Url = "https://www.youtube.com/results?search_query=" + Uri.EscapeDataString("طريقة عمل " + dish.Name)
                });
                foreach (var (key, qty) in Recipe(dish.Name))
                {
                    if (!keyed.TryGetValue(key, out var ingredient)) continue;
                    Add(new ItemContent
                    {
                        ItemId = item.Id,
                        ContentId = ingredient.Id,
                        UnitId = ingredient.UnitId,
                        Quantity = qty
                    });
                }
                added++;
            }

            if (mealLinks.Add((item.Id, meal.Id)))
                Add(new ItemMeal { ItemId = item.Id, MealId = meal.Id });

            if (!linkedIds.Contains(item.Id) && countries.ContainsKey(places.Primary))
            {
                LinkCountries(item, places, countries);
                linkedIds.Add(item.Id);
            }

            if (++batch % 40 == 0)
            {
                await db.SaveChangesAsync();
                Console.WriteLine("MENU " + batch);
            }
        }

        await db.SaveChangesAsync();
        Console.WriteLine("MENU DONE added " + added);
    }

    public async Task MergeItemsAsync()
    {
        var items = await db.Items.Where(e => !e.IsDeleted).ToListAsync();
        var meals = await db.ItemMeals.Where(e => !e.IsDeleted).ToListAsync();
        var countries = await db.ItemCountries.Where(e => !e.IsDeleted).ToListAsync();
        var steps = await db.ItemSteps.Where(e => !e.IsDeleted).ToListAsync();
        var videos = await db.ItemVideos.Where(e => !e.IsDeleted).ToListAsync();
        var contents = await db.ItemContents.Where(e => !e.IsDeleted).ToListAsync();
        var levels = await db.ItemLevels.Where(e => !e.IsDeleted).ToListAsync();
        var favorites = await db.Favorites.Where(e => !e.IsDeleted).ToListAsync();
        var requests = await db.RequestItems.Where(e => !e.IsDeleted).ToListAsync();
        var mealsByItem = meals.GroupBy(e => e.ItemId).ToDictionary(g => g.Key, g => g.ToList());
        var countriesByItem = countries.GroupBy(e => e.ItemId).ToDictionary(g => g.Key, g => g.ToList());
        var stepsByItem = steps.GroupBy(e => e.ItemId).ToDictionary(g => g.Key, g => g.ToList());
        var videosByItem = videos.GroupBy(e => e.ItemId).ToDictionary(g => g.Key, g => g.ToList());
        var contentsByItem = contents.GroupBy(e => e.ItemId).ToDictionary(g => g.Key, g => g.ToList());
        var levelsByItem = levels.GroupBy(e => e.ItemId).ToDictionary(g => g.Key, g => g.ToList());
        var removed = 0;

        foreach (var group in items.GroupBy(item => CleanDishName(item.Name ?? "")))
        {
            var cleaned = group.Key;
            if (string.IsNullOrWhiteSpace(cleaned)) continue;
            var keeper = group
                .OrderBy(item => (item.Name ?? "").Contains("العائل") ? 1 : 0)
                .ThenBy(item => item.Id)
                .First();
            var oldNames = group.Select(item => item.Name ?? "").Where(name => name.Length > 0).Distinct().OrderByDescending(name => name.Length).ToArray();
            keeper.Name = cleaned;
            keeper.Summary = Rewrite(keeper.Summary, oldNames, cleaned);
            keeper.Description = Rewrite(keeper.Description, oldNames, cleaned);
            if (stepsByItem.TryGetValue(keeper.Id, out var keeperSteps))
                foreach (var step in keeperSteps) step.Body = Rewrite(step.Body, oldNames, cleaned);
            if (videosByItem.TryGetValue(keeper.Id, out var keeperVideos))
                foreach (var video in keeperVideos)
                {
                    video.Title = Rewrite(video.Title, oldNames, cleaned);
                    video.Url = "https://www.youtube.com/results?search_query=" + Uri.EscapeDataString("طريقة عمل " + cleaned);
                }
            if (countriesByItem.TryGetValue(keeper.Id, out var keeperCountries))
                foreach (var link in keeperCountries)
                {
                    link.Name = CleanDishName(link.Name ?? cleaned);
                    link.Summary = Rewrite(link.Summary, oldNames, cleaned);
                    link.Description = Rewrite(link.Description, oldNames, cleaned);
                }

            var seenMeals = new HashSet<Guid>();
            foreach (var row in group)
            {
                if (!mealsByItem.TryGetValue(row.Id, out var rowMeals)) continue;
                foreach (var link in rowMeals)
                {
                    if (seenMeals.Add(link.MealId))
                    {
                        if (link.ItemId != keeper.Id)
                        {
                            Add(new ItemMeal { ItemId = keeper.Id, MealId = link.MealId });
                            link.IsDeleted = true;
                        }
                    }
                    else link.IsDeleted = true;
                }
            }

            foreach (var row in group)
            {
                if (row.Id == keeper.Id) continue;
                removed++;
                row.IsDeleted = true;
                Retire(countriesByItem, row.Id);
                Retire(stepsByItem, row.Id);
                Retire(videosByItem, row.Id);
                Retire(contentsByItem, row.Id);
                Retire(levelsByItem, row.Id);
                if (mealsByItem.TryGetValue(row.Id, out var extraMeals))
                    foreach (var link in extraMeals) link.IsDeleted = true;
            }
        }

        var keeperByName = items.Where(item => !item.IsDeleted).GroupBy(item => CleanDishName(item.Name ?? "")).ToDictionary(g => g.Key, g => g.First());
        var keptFavorite = new HashSet<string>();
        foreach (var favorite in favorites)
        {
            var source = items.FirstOrDefault(item => item.Id == favorite.ItemId);
            var cleaned = CleanDishName(source?.Name ?? "");
            if (!keeperByName.TryGetValue(cleaned, out var target)) continue;
            if (!keptFavorite.Add(favorite.UserId + ":" + target.Id))
            {
                favorite.IsDeleted = true;
                continue;
            }
            favorite.ItemId = target.Id;
        }

        foreach (var request in requests)
        {
            var source = items.FirstOrDefault(item => item.Id == request.ItemId);
            if (source == null || !source.IsDeleted) continue;
            var cleaned = CleanDishName(source.Name ?? "");
            if (keeperByName.TryGetValue(cleaned, out var target)) request.ItemId = target.Id;
        }

        var seenLinks = new HashSet<(Guid ItemId, Guid MealId)>();
        var duplicateLinks = 0;
        foreach (var link in db.ChangeTracker.Entries<ItemMeal>().Select(entry => entry.Entity))
        {
            if (link.IsDeleted) continue;
            if (seenLinks.Add((link.ItemId, link.MealId))) continue;
            link.IsDeleted = true;
            duplicateLinks++;
        }

        await db.SaveChangesAsync();
        Console.WriteLine("MERGE removed " + removed + " duplicate-links " + duplicateLinks);
    }

    private static void Retire<T>(Dictionary<Guid, List<T>> rows, Guid id) where T : Foods.Domain.BaseEntity
    {
        if (!rows.TryGetValue(id, out var list)) return;
        foreach (var row in list) row.IsDeleted = true;
    }

    private static string Rewrite(string value, string[] oldNames, string cleaned)
    {
        if (string.IsNullOrEmpty(value)) return value;
        foreach (var oldName in oldNames)
            value = value.Replace(oldName, cleaned);
        value = Regex.Replace(value, @"\s*ضمن وجبة (فطار|غداء|عشاء|حلويات|مشروبات)", "");
        value = Regex.Replace(value, @"في وجبة (فطار|غداء|عشاء|حلويات|مشروبات)", "في وجباتها");
        return Regex.Replace(value, @"\s{2,}", " ").Trim();
    }

    private async Task<Dictionary<string, Meal>> EnsureMealsAsync()
    {
        Directory.CreateDirectory(uploads);
        var map = new Dictionary<string, Meal>();
        foreach (var meal in await db.Meals.Where(e => !e.IsDeleted).ToListAsync())
            if (!string.IsNullOrEmpty(meal.Name))
                map.TryAdd(meal.Name, meal);
        foreach (var row in MealSpecs)
        {
            if (map.ContainsKey(row.Name)) continue;
            var file = Path.Combine(uploads, "meal-" + row.Name + ".png");
            if (!File.Exists(file)) Plate(file, row.Name);
            map[row.Name] = Add(new Meal { Name = row.Name, Description = row.Description, ImagePath = "/uploads/" + Path.GetFileName(file) });
        }
        await db.SaveChangesAsync();
        return map;
    }

    private async Task EnsureGroupAsync(string name)
    {
        if (await db.ItemGroups.AnyAsync(e => !e.IsDeleted && e.Name == name)) return;
        Add(new ItemGroup { Name = name });
        await db.SaveChangesAsync();
    }

    private async Task EnsurePantryAsync()
    {
        var names = (await db.Contents.Where(e => !e.IsDeleted).Select(e => e.Name).ToListAsync()).ToHashSet();
        var units = await db.Units.Where(e => !e.IsDeleted).ToListAsync();
        var places = await db.PurchasePlaces.Where(e => !e.IsDeleted).ToDictionaryAsync(e => e.Name ?? "");
        var categories = await db.ContentCategories.Where(e => !e.IsDeleted).ToDictionaryAsync(e => e.Name ?? "");
        var groups = await db.ContentGroup.Where(e => !e.IsDeleted).ToDictionaryAsync(e => e.Name ?? "");
        foreach (var row in Pantry())
        {
            if (names.Contains(row.Name)) continue;
            var unit = units.FirstOrDefault(e => e.Name == row.Unit || e.NickName == row.Unit);
            if (unit == null || !places.TryGetValue(row.Place, out var place) || !categories.TryGetValue(row.Category, out var category) || !groups.TryGetValue(row.Group, out var group))
                continue;
            var file = Path.Combine(uploads, "ing-" + row.Key + ".png");
            if (!File.Exists(file)) Plate(file, row.Name);
            Add(new Content
            {
                Name = row.Name,
                ContentCategoryId = category.Id,
                ContentGroupId = group.Id,
                UnitId = unit.Id,
                MiniQuantity = row.Mini,
                PurchasePlaceId = place.Id,
                ImagePath = "/uploads/" + Path.GetFileName(file)
            });
            names.Add(row.Name);
        }
        await db.SaveChangesAsync();
    }

    public async Task AlignRecipesAsync()
    {
        await EnsurePantryAsync();
        var byName = await db.Contents.Where(e => !e.IsDeleted && e.Name != null).ToDictionaryAsync(e => e.Name!);
        var keyed = new Dictionary<string, Content>();
        foreach (var row in Pantry())
            if (byName.TryGetValue(row.Name, out var content))
                keyed[row.Key] = content;
        var countries = await db.Countries.Where(e => !e.IsDeleted).ToDictionaryAsync(e => e.Id);
        var items = await db.Items.Where(e => !e.IsDeleted).ToListAsync();
        var stepsBy = (await db.ItemSteps.Where(e => !e.IsDeleted).ToListAsync()).GroupBy(e => e.ItemId).ToDictionary(g => g.Key, g => g.ToList());
        var contentsBy = (await db.ItemContents.Where(e => !e.IsDeleted).ToListAsync()).GroupBy(e => e.ItemId).ToDictionary(g => g.Key, g => g.ToList());
        var linksBy = (await db.ItemCountries.Where(e => !e.IsDeleted).ToListAsync()).GroupBy(e => e.ItemId).ToDictionary(g => g.Key, g => g.ToList());
        var done = 0;
        foreach (var item in items)
        {
            var name = item.Name ?? "";
            if (name.Length == 0) continue;
            var method = Method(name, "");
            var parts = Steps(name, "");
            item.Description = method;
            if (stepsBy.TryGetValue(item.Id, out var oldSteps))
                foreach (var step in oldSteps) step.IsDeleted = true;
            for (var i = 0; i < parts.Length; i++)
                Add(new ItemStep { ItemId = item.Id, Sort = i + 1, Body = parts[i] });
            if (contentsBy.TryGetValue(item.Id, out var oldContents))
                foreach (var row in oldContents) row.IsDeleted = true;
            foreach (var (key, qty) in Recipe(name))
            {
                if (!keyed.TryGetValue(key, out var ingredient)) continue;
                Add(new ItemContent
                {
                    ItemId = item.Id,
                    ContentId = ingredient.Id,
                    UnitId = ingredient.UnitId,
                    Quantity = qty
                });
            }
            if (linksBy.TryGetValue(item.Id, out var links))
                foreach (var link in links)
                {
                    var dialect = countries.TryGetValue(link.CountryId, out var country) ? country.Dialect : null;
                    link.Description = Spoken(method, dialect);
                }
            if (++done % 40 == 0)
            {
                await db.SaveChangesAsync();
                Console.WriteLine("ALIGN " + done);
            }
        }
        await db.SaveChangesAsync();
        Console.WriteLine("ALIGN DONE " + done);
    }

    private async Task<Dictionary<string, Country>> EnsureCountries()
    {
        var map = new Dictionary<string, Country>();
        foreach (var country in await db.Countries.Where(e => !e.IsDeleted).ToListAsync())
            if (!string.IsNullOrEmpty(country.Name))
                map.TryAdd(country.Name, country);

        foreach (var row in CountryIds.All)
        {
            if (map.ContainsKey(row.Name)) continue;
            map[row.Name] = Add(new Country { Id = row.Id, Name = row.Name, Dialect = row.Dialect });
        }

        await db.SaveChangesAsync();
        return map;
    }

    public async Task RefreshImagesAsync()
    {
        Directory.CreateDirectory(uploads);
        var meals = await db.Meals.Where(e => !e.IsDeleted).ToListAsync();
        foreach (var meal in meals)
            meal.ImagePath = await RealPhoto("meal-" + meal.Name, English(meal.Name)) ?? meal.ImagePath;
        var contents = await db.Contents.Where(e => !e.IsDeleted).ToListAsync();
        foreach (var content in contents)
            content.ImagePath = await RealPhoto("ing-" + content.Name, English(content.Name)) ?? content.ImagePath;
        var items = await db.Items.Where(e => !e.IsDeleted).ToListAsync();
        var done = 0;
        foreach (var item in items)
        {
            item.ImagePath = await RealPhoto("dish-" + item.Name, English(item.Name)) ?? item.ImagePath;
            if (++done % 25 == 0)
            {
                await db.SaveChangesAsync();
                Console.WriteLine("IMAGES " + done);
            }
        }
        await db.SaveChangesAsync();
        Console.WriteLine("IMAGES DONE " + items.Count);
    }

    private async Task<string> RealPhoto(string key, string search)
    {
        var safe = string.Concat(key.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '-' : ch));
        var file = Path.Combine(uploads, safe + ".jpg");
        if (File.Exists(file) && new FileInfo(file).Length > 4000 && !LooksLikePlate(file))
            return "/uploads/" + Path.GetFileName(file);
        var url = await Commons(search) ?? await Commons(search.Replace(" food", " filetype:bitmap"));
        if (url != null && await Download(url, file) && new FileInfo(file).Length > 4000)
            return "/uploads/" + Path.GetFileName(file);
        Console.WriteLine("NO PHOTO " + search);
        return null;
    }

    private static bool LooksLikePlate(string file)
    {
        var bytes = File.ReadAllBytes(file);
        return bytes.Length < 8000 && bytes.Length > 8 && bytes[0] == 137 && bytes[1] == 80;
    }

    private static string English(string name)
    {
        var words = new (string Ar, string En)[]
        {
            ("ملوخية", "molokhia"), ("شكشوكة", "shakshuka"), ("مناقيش", "manakish"), ("طعمية", "egyptian falafel"),
            ("فلافل", "falafel"), ("فول", "foul medames"), ("حمص", "hummus"), ("كشري", "koshari"),
            ("بامية", "okra stew"), ("محشي", "stuffed vegetables"), ("كفتة", "kofta"), ("كباب", "kebab"),
            ("مندي", "mandi rice"), ("كبسة", "kabsa"), ("مجبوس", "machboos chicken"), ("مظبي", "madhbi meat"),
            ("هريس", "harees"), ("صيادية", "sayadieh fish rice"), ("شاورما", "shawarma"), ("بشاميل", "pasta bechamel"),
            ("مسقعة", "moussaka"), ("كسكسي", "couscous"), ("طاجين", "moroccan tagine"), ("بسطيلة", "pastilla"),
            ("فتة", "fatteh"), ("كبة", "kibbeh"), ("منسف", "mansaf"), ("مقلوبة", "maqluba"), ("مسخن", "musakhan"),
            ("تبولة", "tabbouleh"), ("فتوش", "fattoush"), ("كنافة", "knafeh"), ("بقلاوة", "baklava"),
            ("بسبوسة", "basbousa"), ("قطايف", "qatayef"), ("لقيمات", "luqaimat"), ("مهلبية", "muhallebi"),
            ("أرز بلبن", "rice pudding"), ("أم علي", "om ali"), ("لبنة", "labneh"), ("مسمن", "msemen"),
            ("بغرير", "baghrir"), ("حرشة", "harcha"), ("بليلة", "wheat porridge"), ("جريش", "jareesh"),
            ("فتوش", "fattoush"), ("بابا غنوج", "baba ganoush"), ("متبل", "mutabal"), ("جمبري", "grilled shrimp"),
            ("سمك", "grilled fish"), ("فراخ", "roast chicken"), ("دجاج", "roast chicken"), ("لحم", "lamb stew"),
            ("لحمة", "beef stew"), ("بيض", "fried eggs"), ("جبن", "white cheese"), ("زبادي", "yogurt"),
            ("شوربة", "lentil soup"), ("حساء", "soup"), ("سلطة", "arabic salad"), ("بطاطس", "potato"),
            ("باذنجان", "eggplant"), ("كبدة", "liver"), ("سجق", "sausage"), ("أرز", "rice dish"),
            ("مكرونة", "pasta"), ("عدس", "lentil"), ("خضار", "vegetables"), ("فطار", "egyptian breakfast"),
            ("غداء", "middle eastern lunch"), ("عشاء", "grilled dinner")
        };
        foreach (var word in words)
            if (name.Contains(word.Ar)) return word.En + " food";
        return "middle eastern food plate";
    }

    private Dictionary<string, Country> SeedCountries()
    {
        var rows = CountryIds.All;
        var map = new Dictionary<string, Country>();
        foreach (var row in rows)
            map[row.Name] = Add(new Country { Id = row.Id, Name = row.Name, Dialect = row.Dialect });
        return map;
    }

    private void LinkCountries(Item item, (string Primary, string[] Also) places, Dictionary<string, Country> countries)
    {
        var (primary, also) = places;
        Add(new ItemCountry
        {
            ItemId = item.Id,
            CountryId = countries[primary].Id,
            IsPrimary = true,
            Name = item.Name,
            Summary = item.Summary,
            Description = item.Description
        });
        foreach (var name in also)
        {
            var country = countries[name];
            Add(new ItemCountry
            {
                ItemId = item.Id,
                CountryId = country.Id,
                IsPrimary = false,
                Name = Spoken(item.Name, country.Dialect),
                Summary = item.Summary,
                Description = item.Description
            });
        }
    }

    private static string? Spoken(string? name, string? dialect)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        var swaps = dialect switch
        {
            "شامي" => new (string From, string To)[] { ("فراخ", "دجاج"), ("طماطم", "بندورة") },
            "خليجي" or "يمني" => new (string From, string To)[] { ("فراخ", "دجاج") },
            "عراقي" => new (string From, string To)[] { ("فراخ", "دجاج"), ("طماطم", "طماطة") },
            "مغربي" => new (string From, string To)[] { ("طماطم", "مطيشة"), ("فراخ", "دجاج"), ("كزبرة", "قصبر") },
            "تونسي" => new (string From, string To)[] { ("فراخ", "دجاج"), ("كزبرة", "كزبر") },
            "جزائري" => new (string From, string To)[] { ("طماطم", "طماطيش"), ("فراخ", "دجاج") },
            "ليبي" or "حساني" or "سوداني" => new (string From, string To)[] { ("فراخ", "دجاج") },
            _ => Array.Empty<(string From, string To)>()
        };
        var text = name;
        foreach (var swap in swaps)
            text = text.Replace(swap.From, swap.To);
        return text;
    }

    private T Add<T>(T entity) where T : Foods.Domain.BaseEntity
    {
        entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
        entity.IsDeleted = false;
        entity.Status = 1;
        db.Add(entity);
        return entity;
    }

    private static User User(Guid roleId, string userName, string password, string name) => new()
    {
        Id = userName == "Admin" ? Guid.Parse("111d7bfe-8dda-4af9-b651-f4a8d9008b94") : Guid.NewGuid(),
        RoleId = roleId,
        UserName = userName,
        Password = password,
        Name = name
    };

    private Dictionary<string, T> Map<T>(IEnumerable<string> names, Func<string, T> create) where T : Foods.Domain.BaseEntity
    {
        var map = new Dictionary<string, T>();
        foreach (var name in names)
        {
            var entity = create(name);
            if (entity is Foods.Domain.BaseMasterDataEntity master) master.Name = name;
            map[name] = Add(entity);
        }
        return map;
    }

    private static string Group(string name)
    {
        if (name.Contains("شاي") || name.Contains("قهوة") || name.Contains("عصير") || name.Contains("كركديه") || name.Contains("عرقسوس") || name.Contains("سحلب") || name.Contains("سوبيا") || name.Contains("خروب") || name.Contains("قمر الدين") || name.Contains("عيران") || name.Contains("لبن رايب") || name.Contains("كرك") || name.Contains("جلّاب") || name.Contains("ينسون") || name.Contains("كراوية") || name.Contains("حلبة") || name.Contains("زهورات") || name.Contains("بابونج") || name.Contains("مريمية") || name.Contains("كاكاو") || name.Contains("شوكليت") || name.Contains("شراب") || name.Contains("أتاي") || name.Contains("قشر") || name.Contains("مغلي") || name.Contains("مشروب") || name.Contains("ميلك") || name.Contains("آيس") || name.Contains("ليمون بالنعناع"))
            return "مشروبات";
        if (name.Contains("شوربة") || name.Contains("حساء")) return "شوربات";
        if (name.Contains("مشوي") || name.Contains("كباب") || name.Contains("كفتة") || name.Contains("شيش")) return "مشويات";
        if (name.Contains("سلطة") || name.Contains("تبولة") || name.Contains("فتوش")) return "سلطات";
        if (name.Contains("كنافة") || name.Contains("بقلاوة") || name.Contains("بسبوسة") || name.Contains("قطايف") || name.Contains("لقيمات") || name.Contains("أم علي") || name.Contains("مهلبية") || name.Contains("أرز بلبن") || name.Contains("رز بحليب") || name.Contains("حلاوة") || name.Contains("معمول") || name.Contains("غريبة") || name.Contains("كعك") || name.Contains("بلح الشام") || name.Contains("زنود") || name.Contains("عيش السرايا") || name.Contains("هريسة") || name.Contains("نمورة") || name.Contains("زلابية") || name.Contains("عوامة") || name.Contains("مشبك") || name.Contains("مقروط") || name.Contains("سفوف") || name.Contains("سلو") || name.Contains("خشاف") || name.Contains("جزرية") || name.Contains("برازق") || name.Contains("قراشل") || name.Contains("فقاص"))
            return "حلويات";
        if (name.Contains("خبز") || name.Contains("مناقيش") || name.Contains("فطير") || name.Contains("مسمن") || name.Contains("رقاق") || name.Contains("حرشة") || name.Contains("بغرير")) return "مخبوزات";
        if (name.Contains("فول") || name.Contains("طعمية") || name.Contains("فلافل") || name.Contains("بيض") || name.Contains("شكشوكة")) return "فطور شعبي";
        return "أطباق رئيسية";
    }

    private static string Method(string name, string meal)
    {
        var list = string.Join("، ", Recipe(name).Select(row => row.Qty + " " + row.Key));
        return "مقادير " + name + " هي: " + list + ". " + Technique(name) + ".";
    }

    private static string[] Steps(string name, string meal)
    {
        var list = string.Join("، ", Recipe(name).Select(row => row.Qty + " " + row.Key));
        return new[]
        {
            "جهّز مقادير " + name + " فقط: " + list + ".",
            Technique(name) + ".",
            Finish(name)
        };
    }

    private static string Technique(string name)
    {
        if (name.Contains("أرز بلبن") || name.Contains("رز بحليب"))
            return "اغسل الأرز ثم ضعه مع اللبن والسكر على نار هادئة وقلّب حتى يتشرب اللبن ويثقل" + (name.Contains("سمن") ? "، وأضف السمن في آخر التقليب" : "");
        if (name.Contains("مهلبية") || name.Contains("سحلب"))
            return "ذوّب النشا في قليل من اللبن البارد، ثم أضفه لباقي اللبن والسكر على نار هادئة وقلّب حتى يثقل من غير كتل";
        if (name.Contains("أم علي"))
            return "رصّ الرقاق في صينية، ووزّع عليه اللبن والسكر والمكسرات، ثم ادخله الفرن حتى يحمر الوجه";
        if (Group(name) == "مشروبات")
        {
            if (name.Contains("مثلج") || name.Contains("آيس") || name.Contains("بالثلج"))
                return "حضّر المشروب من مقاديره ثم صفّه وقدّمه باردًا على الثلج";
            if (name.Contains("عصير"))
                return "اغسل الفاكهة واعصرها أو اخلطها، ثم صفّ العصير وأضف التحلية من مقاديره";
            return "سخّن الماء أو اللبن، وأضف المكوّن الأساسي من المقادير واتركه ينقع أو يغلي دقائق ثم صفّه";
        }
        if (name.Contains("كنافة") || name.Contains("بقلاوة") || name.Contains("بسبوسة") || name.Contains("هريسة") || name.Contains("نمورة"))
            return "وزّع العجين أو السميد في الصينية وادهنه بالسمن، واخبزه حتى يتحمر، ثم اسقه القطر وهو ساخن";
        if (name.Contains("قطايف") || name.Contains("لقيمات") || name.Contains("زلابية") || name.Contains("عوامة") || name.Contains("بلح الشام") || name.Contains("مشبك"))
            return "اخلط العجين واتركه يرتاح، ثم اقله حتى يتحمر واسقه القطر وهو ساخن";
        if (name.Contains("معمول") || name.Contains("كعك") || name.Contains("غريبة") || name.Contains("برازق"))
            return "اعجن الدقيق بالسمن والحشو المذكور في المقادير، ثم شكّل القطع واخبزها حتى تتحمر";
        if (Group(name) == "حلويات")
            return "اطبخ الحلو من المقادير المذكورة على نار هادئة أو في الفرن حتى ينضج قوامه";
        if (name.Contains("بالفرن"))
            return "تبّل المقادير وصفّها في صينية، ثم ادخلها الفرن حتى تنضج وتحمر";
        if (name.Contains("فحم") || name.Contains("مشوي") || name.Contains("كباب") || name.Contains("كفتة") || name.Contains("شيش"))
            return "تبّل المقادير واتركها تتشرب، ثم اشوها على نار هادئة حتى تنضج";
        if (name.Contains("شوربة") || name.Contains("حساء"))
            return "ضع مقادير الشوربة في الحلة وغطّها بالماء، واتركها تسلق حتى تطيب ثم اضبط الملح";
        if (name.Contains("سلطة") || name.Contains("تبولة") || name.Contains("فتوش"))
            return "اغسل الخضار وقطّعها، ثم اخلطها بمقادير التتبيلة من غير نار";
        if (name.Contains("محشي"))
            return "احفر الخضار واحشها بخليط الأرز واللحم من المقادير، ثم رصّها في الحلة مع الصلصة حتى تستوي";
        if (name.Contains("أرز") || name.Contains("كبسة") || name.Contains("مندي") || name.Contains("مجبوس") || name.Contains("مظبي"))
            return "حمّر اللحم أو الفراخ من المقادير، ثم انزل الأرز والبهارات واتركه يتشرب المرق حتى يستوي";
        return "اطبخ " + name + " من المقادير المذكورة فقط على نار متوسطة حتى تنضج، من غير إضافة مكونات خارج القائمة";
    }

    private static string Finish(string name)
    {
        if (Group(name) == "مشروبات") return "قدّم " + name + " في كوب بعد التصفية.";
        if (Group(name) == "حلويات") return "قدّم " + name + " بعد أن يهدأ، من غير خبز أو أرز.";
        if (name.Contains("سلطة") || name.Contains("تبولة") || name.Contains("فتوش")) return "قدّم " + name + " فور الخلط.";
        return "قدّم " + name + " ساخنًا.";
    }

    private static List<(string Key, decimal Qty)> Recipe(string name)
    {
        var lines = BaseRecipe(name).ToList();
        void add(string key, decimal qty)
        {
            if (lines.All(line => line.Key != key)) lines.Add((key, qty));
        }
        if (name.Contains("سمن")) add("سمن", 20);
        if (name.Contains("ليمون")) add("ليمون", 15);
        if (name.Contains("ثوم")) add("ثوم", 10);
        if (name.Contains("قطر")) add("قطر", 30);
        if (name.Contains("مكسرات")) add("مكسرات", 20);
        if (name.Contains("فستق")) add("فستق", 15);
        if (name.Contains("هيل")) add("هيل", 2);
        if (name.Contains("سمسم")) add("سمسم", 10);
        if (name.Contains("نعناع")) add("نعناع", 5);
        if (name.Contains("قشطة")) add("قشطة", 40);
        if (name.Contains("بالحليب") || name.Contains("باللبن")) add("لبن", 150);
        if (name.Contains("بهار")) add("بهار", 5);
        if (name.Contains("الحارة")) add("فلفل أسود", 2);
        if (name.Contains("بدون سكر")) lines.RemoveAll(line => line.Key == "سكر");
        if (name.Contains("قليل السكر") || name.Contains("قليلة السكر"))
            for (var i = 0; i < lines.Count; i++)
                if (lines[i].Key == "سكر") lines[i] = ("سكر", Math.Max(5, lines[i].Qty / 2));
        return lines;
    }

    private static IEnumerable<(string Key, decimal Qty)> BaseRecipe(string name)
    {
        var rules = new (string Word, (string Key, decimal Qty)[] Lines)[]
        {
            ("فول", A(("فول", 200), ("كمون", 5), ("ليمون", 15), ("زيت", 20), ("طماطم", 80))),
            ("طعمية", A(("فول", 180), ("بقدونس", 20), ("كزبرة", 15), ("سمسم", 10), ("زيت", 40))),
            ("فلافل", A(("حمص", 180), ("بقدونس", 20), ("كمون", 5), ("زيت", 40))),
            ("حمص", A(("حمص", 200), ("طحينة", 30), ("ليمون", 20), ("ثوم", 10))),
            ("شكشوكة", A(("بيض", 2), ("طماطم", 150), ("فلفل", 50), ("بصل", 40))),
            ("بيض", A(("بيض", 2), ("زيت", 15), ("طماطم", 60), ("ملح", 2))),
            ("مناقيش", A(("دقيق", 150), ("زيت", 20), ("زعتر", 10), ("جبنة", 40))),
            ("جبن", A(("جبنة", 80), ("زيت", 10), ("زعتر", 3), ("خبز", 1))),
            ("لبنة", A(("لبنة", 100), ("زيت", 15), ("زعتر", 3), ("خبز", 1))),
            ("زبادي", A(("زبادي", 150), ("عسل", 20), ("خيار", 50))),
            ("بليلة", A(("قمح", 80), ("لبن", 200), ("سكر", 15))),
            ("عسل", A(("عسل", 30), ("طحينة", 30), ("خبز", 1))),
            ("فطير", A(("دقيق", 150), ("سمن", 20), ("عسل", 25))),
            ("مسمن", A(("دقيق", 150), ("زيت", 20), ("عسل", 20))),
            ("كشري", A(("أرز", 80), ("مكرونة", 80), ("عدس", 60), ("حمص", 40), ("صلصة", 50), ("بصل", 40))),
            ("ملوخية", A(("ملوخية", 150), ("فراخ", 200), ("ثوم", 15), ("أرز", 100))),
            ("محشي", A(("أرز", 80), ("لحمة", 80), ("بصل", 40), ("صلصة", 60), ("نعناع", 5))),
            ("بامية", A(("بامية", 200), ("لحمة", 120), ("صلصة", 50), ("ثوم", 10))),
            ("كفتة", A(("لحمة", 200), ("بصل", 40), ("بقدونس", 15), ("بهار", 5))),
            ("كباب", A(("لحمة", 200), ("بصل", 40), ("بهار", 5), ("فلفل أسود", 2))),
            ("مندي", A(("أرز", 150), ("لحمة", 200), ("بهار", 5), ("بصل", 40))),
            ("كبسة", A(("أرز", 150), ("فراخ", 200), ("جزر", 40), ("بهار", 5))),
            ("مجبوس", A(("أرز", 150), ("فراخ", 200), ("بهار", 5), ("بصل", 30))),
            ("مظبي", A(("أرز", 150), ("لحمة", 200), ("بهار", 5))),
            ("هريس", A(("قمح", 120), ("لحمة", 150), ("سمن", 20))),
            ("صيادية", A(("سمك", 250), ("أرز", 150), ("بصل", 80), ("كمون", 5))),
            ("سمك", A(("سمك", 250), ("ليمون", 20), ("كمون", 5), ("ثوم", 10))),
            ("جمبري", A(("جمبري", 200), ("ثوم", 15), ("ليمون", 15), ("زيت", 20))),
            ("بشاميل", A(("مكرونة", 120), ("لبن", 200), ("لحمة", 100), ("دقيق", 20))),
            ("مسقعة", A(("باذنجان", 200), ("صلصة", 80), ("فلفل", 40), ("ثوم", 10))),
            ("كسكسي", A(("كسكسي", 150), ("خضار", 120), ("لحمة", 120), ("حمص", 40))),
            ("طاجين", A(("لحمة", 180), ("برقوق", 40), ("بصل", 40), ("بهار", 5))),
            ("بسطيلة", A(("فراخ", 180), ("لوز", 30), ("دقيق", 80), ("بصل", 40))),
            ("شاورما", A(("فراخ", 200), ("ثوم", 15), ("خبز", 1), ("طحينة", 20))),
            ("فتة", A(("خبز", 1), ("أرز", 80), ("لحمة", 100), ("ثوم", 10), ("سمن", 15))),
            ("كبة", A(("برغل", 100), ("لحمة", 120), ("بصل", 40), ("بهار", 5))),
            ("منسف", A(("أرز", 150), ("لحم غنم", 200), ("لبن", 150), ("لوز", 20))),
            ("مقلوبة", A(("أرز", 150), ("باذنجان", 100), ("لحمة", 150), ("بطاطس", 80))),
            ("مسخن", A(("فراخ", 200), ("خبز", 1), ("بصل", 100), ("سماق", 5))),
            ("شوربة", A(("عدس", 80), ("بصل", 40), ("كمون", 5), ("ليمون", 15))),
            ("حساء", A(("عدس", 80), ("جزر", 40), ("بصل", 30), ("كمون", 5))),
            ("سلطة", A(("طماطم", 80), ("خيار", 80), ("بقدونس", 20), ("ليمون", 15), ("زيت", 15))),
            ("تبولة", A(("برغل", 40), ("بقدونس", 50), ("طماطم", 60), ("ليمون", 20))),
            ("فتوش", A(("خبز", 1), ("خس", 40), ("طماطم", 60), ("خيار", 50), ("سماق", 3))),
            ("كنافة", A(("دقيق", 100), ("جبنة", 80), ("سمن", 20), ("قطر", 40))),
            ("بقلاوة", A(("دقيق", 100), ("فستق", 30), ("سمن", 25), ("قطر", 40))),
            ("بسبوسة", A(("سميد", 120), ("سكر", 40), ("سمن", 30), ("قطر", 40))),
            ("قطايف", A(("دقيق", 100), ("جوز", 30), ("قطر", 40))),
            ("لقيمات", A(("دقيق", 100), ("سكر", 20), ("زيت", 40), ("قطر", 30))),
            ("مهلبية", A(("لبن", 250), ("نشا", 20), ("سكر", 30))),
            ("أرز بلبن", A(("أرز", 60), ("لبن", 250), ("سكر", 30))),
            ("رز بحليب", A(("أرز", 60), ("لبن", 250), ("سكر", 30))),
            ("أم علي", A(("لبن", 250), ("رقاق", 40), ("مكسرات", 20), ("سكر", 25))),
            ("قهوة", A(("قهوة", 15), ("هيل", 2), ("سكر", 10), ("لبن", 50))),
            ("شاي", A(("شاي", 8), ("نعناع", 5), ("سكر", 10))),
            ("كركديه", A(("كركديه", 20), ("سكر", 15), ("نعناع", 3))),
            ("تمر هندي", A(("تمر هندي", 30), ("سكر", 15), ("ليمون", 10))),
            ("عرقسوس", A(("عرقسوس", 20), ("سكر", 10))),
            ("سحلب", A(("لبن", 200), ("نشا", 15), ("سكر", 20), ("قرفة", 2))),
            ("سوبيا", A(("أرز", 40), ("لبن", 200), ("سكر", 20), ("جوز هند", 10))),
            ("خروب", A(("خروب", 25), ("سكر", 15), ("لبن", 50))),
            ("قمر الدين", A(("قمر الدين", 40), ("سكر", 15), ("لبن", 50))),
            ("عصير", A(("سكر", 15), ("ليمون", 10), ("نعناع", 3))),
            ("عيران", A(("لبن", 200), ("ملح", 2), ("نعناع", 3))),
            ("جلّاب", A(("قطر", 30), ("فستق", 10), ("سكر", 10)))
        };
        foreach (var rule in rules)
            if (name.Contains(rule.Word)) return rule.Lines;
        if (Group(name) == "حلويات") return A(("دقيق", 120), ("سكر", 40), ("سمن", 25));
        if (Group(name) == "مشروبات") return A(("سكر", 15), ("ليمون", 10));
        return A(("بصل", 50), ("طماطم", 80), ("زيت", 15), ("ثوم", 10), ("ملح", 2), ("كمون", 3));
    }

    private static (string Key, decimal Qty)[] A(params (string Key, decimal Qty)[] rows) => rows;

    private async Task<string> Picture(string key, string search)
    {
        var safe = string.Concat(key.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '-' : ch));
        var file = Path.Combine(uploads, safe + ".jpg");
        var png = Path.ChangeExtension(file, ".png");
        if (File.Exists(file)) return "/uploads/" + Path.GetFileName(file);
        if (File.Exists(png)) return "/uploads/" + Path.GetFileName(png);
        var url = await Commons(search);
        if (url != null && await Download(url, file)) return "/uploads/" + Path.GetFileName(file);
        Plate(png, search);
        return "/uploads/" + Path.GetFileName(png);
    }

    private async Task<string> Commons(string search)
    {
        try
        {
            var address = "https://commons.wikimedia.org/w/api.php?action=query&format=json&generator=search&gsrnamespace=6&gsrlimit=12&prop=imageinfo&iiprop=url|mime&iiurlwidth=640&gsrsearch="
                + Uri.EscapeDataString(search);
            using var stream = await http.GetStreamAsync(address);
            using var doc = await JsonDocument.ParseAsync(stream);
            if (!doc.RootElement.TryGetProperty("query", out var query) || !query.TryGetProperty("pages", out var pages)) return null;
            foreach (var page in pages.EnumerateObject())
            {
                if (!page.Value.TryGetProperty("imageinfo", out var info)) continue;
                var first = info.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.Undefined) continue;
                var thumbUrl = first.TryGetProperty("thumburl", out var thumb) ? thumb.GetString() : null;
                var fullUrl = first.TryGetProperty("url", out var url) ? url.GetString() : null;
                var chosen = thumbUrl ?? fullUrl;
                if (string.IsNullOrEmpty(chosen)) continue;
                var lower = chosen.ToLowerInvariant();
                if (lower.Contains(".jpg") || lower.Contains(".jpeg") || lower.Contains(".png") || lower.Contains(".webp"))
                    return chosen;
            }
        }
        catch
        {
            return null;
        }
        return null;
    }

    private async Task<bool> Download(string url, string file)
    {
        try
        {
            var bytes = await http.GetByteArrayAsync(url);
            if (bytes.Length < 800) return false;
            await File.WriteAllBytesAsync(file, bytes);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void Plate(string file, string text)
    {
        var seed = text.Aggregate(0, (hash, ch) => hash * 31 + ch);
        const int width = 96;
        const int height = 96;
        var raw = new byte[height * (1 + width * 3)];
        for (var y = 0; y < height; y++)
        {
            var row = y * (1 + width * 3);
            raw[row] = 0;
            for (var x = 0; x < width; x++)
            {
                var plate = (x - 48) * (x - 48) + (y - 52) * (y - 52) < 1600;
                var i = row + 1 + x * 3;
                raw[i] = (byte)(plate ? 190 + (seed % 60) : 255);
                raw[i + 1] = (byte)(plate ? 110 + ((seed / 5) % 70) : 246);
                raw[i + 2] = (byte)(plate ? 50 + ((seed / 11) % 50) : 232);
            }
        }
        using var output = File.Create(file);
        WritePng(output, width, height, raw);
    }

    private static void WritePng(Stream output, int width, int height, byte[] raw)
    {
        var signature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        output.Write(signature);
        Chunk(output, "IHDR", new byte[]
        {
            (byte)(width >> 24), (byte)(width >> 16), (byte)(width >> 8), (byte)width,
            (byte)(height >> 24), (byte)(height >> 16), (byte)(height >> 8), (byte)height,
            8, 2, 0, 0, 0
        });
        using var compressed = new MemoryStream();
        using (var deflate = new DeflateStream(compressed, CompressionLevel.Fastest, true))
            deflate.Write(raw);
        var zlib = new byte[compressed.Length + 6];
        zlib[0] = 0x78;
        zlib[1] = 0x01;
        compressed.Position = 0;
        compressed.Read(zlib, 2, (int)compressed.Length);
        var sum = Adler(raw);
        var end = zlib.Length - 4;
        zlib[end] = (byte)(sum >> 24);
        zlib[end + 1] = (byte)(sum >> 16);
        zlib[end + 2] = (byte)(sum >> 8);
        zlib[end + 3] = (byte)sum;
        Chunk(output, "IDAT", zlib);
        Chunk(output, "IEND", Array.Empty<byte>());
    }

    private static void Chunk(Stream output, string type, byte[] data)
    {
        var typeBytes = Encoding.ASCII.GetBytes(type);
        WriteInt(output, data.Length);
        output.Write(typeBytes);
        output.Write(data);
        var crcSource = typeBytes.Concat(data).ToArray();
        WriteInt(output, (int)Crc(crcSource));
    }

    private static void WriteInt(Stream output, int value)
    {
        output.Write(new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value });
    }

    private static uint Adler(byte[] data)
    {
        uint a = 1, b = 0;
        foreach (var value in data)
        {
            a = (a + value) % 65521;
            b = (b + a) % 65521;
        }
        return (b << 16) | a;
    }

    private static uint Crc(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var value in data)
        {
            crc ^= value;
            for (var i = 0; i < 8; i++)
                crc = (crc & 1) == 1 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }
        return ~crc;
    }

    private static readonly string[] MealNames = { "فطار", "غداء", "عشاء", "حلويات", "مشروبات" };

    private static readonly (string Name, string Description)[] MealSpecs =
    {
        ("فطار", "وجبة الصباح: فول ومخبوزات وبيض وألبان خفيفة من المشرق والمغرب."),
        ("غداء", "وجبة الغداء: أطباق الرز واللحوم والخضار المطهوة على مائدة شرقية."),
        ("عشاء", "وجبة العشاء: مشويات وشوربات وأطباق أخف تُقدَّم في آخر اليوم."),
        ("حلويات", "حلويات شرقية: كنافة وقطايف وبقلاوة وبسبوسة ومهلبية."),
        ("مشروبات", "مشروبات شرقية: شاي وقهوة وكركديه وعصائر وتمر هندي.")
    };

    private static readonly Regex MealWord = new(@"(?<!وجبة\s)(?<=^|\s)(فطار|غداء|عشاء|حلويات|مشروبات|العائلية|العائلي)(?=\s|$)", RegexOptions.Compiled);

    private static string CleanDishName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var cleaned = MealWord.Replace(value, " ");
        return Regex.Replace(cleaned, @"\s{2,}", " ").Trim();
    }

    private static readonly string[] Styles =
    {
        "",
        " بالفرن",
        " على الفحم",
        " بالحلة",
        " بالسمنة البلدي",
        " بالليمون والثوم",
        " الحارة",
        " الخفيفة",
        " بالبهارات",
        " بالسمسم"
    };

    private static readonly string[] SweetStyles =
    {
        "", " بالقطر", " بالمكسرات", " بالقشطة", " بالفرن", " بالفستق", " بالسمسم", " قليلة السكر", " بالسمنة البلدي", " بالهيل"
    };

    private static readonly string[] DrinkStyles =
    {
        "", " بالنعناع", " بالثلج", " بالحليب", " بالهيل", " قليل السكر", " بالسمسم", " بالليمون", " بدون سكر", " الفاتر"
    };

    private static List<Dish> Dishes()
    {
        var list = new List<Dish>();
        list.AddRange(Expand("فطار", Breakfast, Styles));
        list.AddRange(Expand("غداء", Lunch, Styles));
        list.AddRange(Expand("عشاء", Dinner, Styles));
        list.AddRange(Expand("حلويات", Desserts, SweetStyles));
        list.AddRange(Expand("مشروبات", Drinks, DrinkStyles));
        return list;
    }

    private static IEnumerable<Dish> Expand(string meal, string[] rows, string[] styles)
    {
        for (var i = 0; i < 1000; i++)
        {
            var parts = rows[i % rows.Length].Split('|');
            var style = styles[(i / rows.Length) % styles.Length];
            var cycle = i / (rows.Length * styles.Length);
            var name = CleanDishName(parts[0] + style + (cycle > 0 ? " " + (cycle + 1) : ""));
            yield return new Dish(name, parts[1], meal);
        }
    }

    private static (string Primary, string[] Also) CountriesFor(string name, string region)
    {
        if (name.Contains("منسف")) return ("الأردن", Except(Levant, "الأردن"));
        if (name.Contains("مسخن") || name.Contains("نابلسية") || name.Contains("قدرة")) return ("فلسطين", Except(Levant, "فلسطين"));
        if (name.Contains("تبولة") || name.Contains("فتوش") || name.Contains("كبة") || name.Contains("مناقيش")) return ("لبنان", Except(Levant, "لبنان"));
        if (name.Contains("مجبوس")) return ("الكويت", Except(Gulf, "الكويت"));
        if (name.Contains("مندي") || name.Contains("مظبي") || name.Contains("يمني") || name.Contains("مطبق")) return ("اليمن", Except(Gulf, "اليمن"));
        if (name.Contains("مضغوط") || name.Contains("مرقوق") || name.Contains("قرصان") || name.Contains("هريس") || name.Contains("جريش") || name.Contains("عصيدة") || name.Contains("كبسة")) return ("السعودية", Except(Gulf, "السعودية"));
        if (name.Contains("تونسي") || name.Contains("بريك")) return ("تونس", Except(Maghreb, "تونس"));
        if (name.Contains("شخشوخ")) return ("الجزائر", Except(Maghreb, "الجزائر"));
        if (name.Contains("كسكسي") || name.Contains("طاجين") || name.Contains("بسطيلة") || name.Contains("مسمن") || name.Contains("حرشة") || name.Contains("بغرير") || name.Contains("مليوي")) return ("المغرب", Except(Maghreb, "المغرب"));
        if (name.Contains("كشري") || name.Contains("ملوخية") || name.Contains("طعمية") || name.Contains("إسكندراني")) return ("مصر", Except(Nile, "مصر"));
        return region switch
        {
            "شامي" => ("سوريا", Except(Levant, "سوريا")),
            "خليجي" => ("السعودية", Except(Gulf, "السعودية")),
            "مغاربي" => ("المغرب", Except(Maghreb, "المغرب")),
            _ => ("مصر", Except(Nile, "مصر"))
        };
    }

    private static readonly string[] Levant = { "سوريا", "لبنان", "فلسطين", "الأردن", "العراق" };
    private static readonly string[] Gulf = { "السعودية", "الإمارات", "الكويت", "البحرين", "قطر", "عُمان", "اليمن" };
    private static readonly string[] Maghreb = { "المغرب", "تونس", "الجزائر", "ليبيا", "موريتانيا" };
    private static readonly string[] Nile = { "مصر", "السودان" };

    private static string[] Except(string[] names, string primary) => names.Where(name => name != primary).ToArray();

    private Dictionary<string, string> IndexPhotos()
    {
        var index = new Dictionary<string, string>();
        if (!Directory.Exists(uploads)) return index;
        foreach (var file in Directory.GetFiles(uploads, "dish-*.jpg"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (name.StartsWith("dish-")) name = name.Substring("dish-".Length);
            index[name] = "/uploads/" + Path.GetFileName(file);
        }
        return index;
    }

    private string PhotoFor(Dictionary<string, string> photos, string meal, string dishName)
    {
        string best = null;
        var bestLength = 0;
        foreach (var photo in photos)
        {
            if (dishName.Contains(photo.Key) && photo.Key.Length > bestLength)
            {
                best = photo.Value;
                bestLength = photo.Key.Length;
            }
        }
        if (best != null) return best;

        var safe = string.Concat(("dish-" + meal + "-" + dishName).Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '-' : ch));
        var png = Path.Combine(uploads, safe + ".png");
        if (!File.Exists(png)) Plate(png, dishName);
        return "/uploads/" + Path.GetFileName(png);
    }

    private static readonly string[] Breakfast =
    {
        "فول مدمس|مصري","فول بالطحينة|مصري","فول بالزيت الحار|مصري","فول بالصلصة|مصري","فول بالبيض|مصري",
        "طعمية|مصري","ساندويتش طعمية|مصري","فلافل محشية|شامي","فلافل بالحمص|شامي","حمص بالطحينة|شامي",
        "شكشوكة مصرية|مصري","شكشوكة تونسية|مغاربي","شكشوكة بالفلفل|مغاربي","بيض عيون|مصري","بيض أومليت|مصري",
        "بيض تركي|شامي","بيض بالبسطرمة|مصري","بيض بالسجق|مصري","بيض بالطماطم|مصري","عجة خضار|شامي",
        "عجة بطاطس|مصري","عجة كوسة|شامي","فتة بالسمن|مصري","فتة بالثوم|مصري","جبنة قريش|مصري",
        "جبنة بيضاء|شامي","جبنة حلوم|شامي","لبنة وزعتر|شامي","لبنة بالزيت|شامي","مناقيش زعتر|شامي",
        "مناقيش جبنة|شامي","مناقيش محمرة|شامي","مناقيش سجق|شامي","مناقيش بالبيض|شامي","صفيحة صغيرة|شامي",
        "لبن رايب|مصري","زبادي بالعسل|مصري","زبادي بالخيار|شامي","بليلة|مصري","حمصة بالسمن|خليجي",
        "عسل وطحينة|مصري","قشطة بالعسل|مصري","مشلتت|مصري","فطير مشلتت|مصري","رقاق بالسمن|مصري",
        "مسقعة بالبيض|مصري","باذنجان مقلي|مصري","بطاطس بالبيض|مصري","سجق بلدي|مصري","كبدة إسكندراني|مصري",
        "كبدة بالثوم|شامي","فول إسكندراني|مصري","فول دمشقي|شامي","فول نابت|مصري","زيتون ومخلل|شامي",
        "جبنة عكاوي|شامي","مسمن مغربي|مغاربي","مسمن بالعسل|مغاربي","مليوي|مغاربي","حرشة|مغاربي",
        "بغرير|مغاربي","بغرير بالعسل|مغاربي","مطبق يمني|خليجي","هريس فطور|خليجي","جريش خفيف|خليجي",
        "تشريبة لبن|خليجي","لقيمات الصباح|خليجي","خبز بالتمر|خليجي","تمر بالحليب|خليجي","بلح باللبن|مصري",
        "شوفان بالحليب|مصري","توست جبنة|مصري","خبز محمص|مصري","سلطة بلدي|مصري","خيار باللبن|شامي",
        "جرجير وزيتون|شامي","كعك بالسمسم|شامي","قرص بالتمر|خليجي","سمبوسة جبن|خليجي","سمبوسة بيض|خليجي",
        "فول بالليمون|مصري","فول بالبصل|مصري","طحينة بالليمون|شامي","فتة حمص|شامي","خبز رقاق|مصري",
        "جبنة فيتا|شامي","أومليت بالجبن|مصري","بيض بالزعتر|شامي","طاجين البيض|مغاربي","بيض بالخضار|شامي",
        "كفتة فطور|مصري","بسطرمة وبيض|مصري","نقانق مشوية|خليجي","فول بالكمون|مصري","حمص بالبقدونس|شامي",
        "مناقيش قشقوان|شامي","جبنة مشوية|خليجي","لبن زبادي سادة|مصري","عسل أبيض|مصري","زيت زيتون وزعتر|شامي"
    };

    private static readonly string[] Lunch =
    {
        "كشري|مصري","ملوخية بالفراخ|مصري","ملوخية بالأرانب|مصري","محشي كرنب|مصري","محشي ورق عنب|شامي",
        "محشي كوسة|مصري","محشي فلفل|مصري","محشي باذنجان|شامي","بامية باللحمة|مصري","طاجن بامية|مصري",
        "كفتة مشوية|مصري","كفتة داوود باشا|شامي","كباب حلة|مصري","كباب مشوي|شامي","شيش طاووق|شامي",
        "فراخ مشوية|مصري","فراخ بانيه|مصري","بط بالبرتقال|مصري","حمام محشي|مصري","مكرونة بشاميل|مصري",
        "طاجن مكرونة|مصري","مسقعة|مصري","فتة شاورما|شامي","شاورما فراخ|شامي","شاورما لحمة|شامي",
        "مندي لحم|خليجي","مندي دجاج|خليجي","كبسة دجاج|خليجي","كبسة لحم|خليجي","مجبوس دجاج|خليجي",
        "مجبوس روبيان|خليجي","مظبي لحم|خليجي","هريس|خليجي","صيادية سمك|مصري","سمك مشوي|مصري",
        "طاجن سمك|مصري","جمبري مشوي|خليجي","جمبري بالثوم|خليجي","كبدة فراخ|مصري","فتة لحمة|مصري",
        "فتة مكدوس|شامي","كبة مقلية|شامي","كبة مشوية|شامي","كبة نية|شامي","يبرق|شامي",
        "منسف|شامي","مقلوبة باذنجان|شامي","مقلوبة زهرة|شامي","مسخن|شامي","قدرة الخليل|شامي",
        "كسكسي باللحم|مغاربي","كسكسي بالخضار|مغاربي","طاجين لحم|مغاربي","طاجين دجاج|مغاربي","بسطيلة|مغاربي",
        "شخشوخة|مغاربي","رز معمر|مغاربي","ملوخية تونسية|مغاربي","عدس بجبة|مصري","كوارع|مصري",
        "فتة كوارع|مصري","رقاق باللحمة|مصري","طاجن بسلة|مصري","بسلة بالجزر|مصري","فاصوليا بيضاء|مصري",
        "فاصوليا خضراء|مصري","سبانخ باللحمة|مصري","قلقاس بالسلق|مصري","أرز بالشعرية|مصري","أرز باللبن مش وحلو|مصري",
        "أرز صيادية|شامي","دجاج محمر|مغاربي","دجاج بالزيتون|مغاربي","مرقة خضار|مغاربي","مفتول|شامي",
        "منسف خضار|شامي","فريكة بالدجاج|شامي","فريكة باللحم|شامي","برغل بالطماطم|شامي","مجدرة|شامي",
        "مجدّرة بعدس|شامي","كبة بطاطس|شامي","صفيحة لحمة|شامي","لحمة بالصينية|شامي","ورق لسان|شامي",
        "شيخ المحشي|مصري","كوسة بالبشاميل|مصري","بطاطس باللحمة|مصري","سنكردة|مصري","سجق بالصلصة|مصري",
        "طاجن عكاوي|مصري","سمك بلتية|مصري","قولون محشي|مصري","ممبار|مصري","فئة بطاطس|مصري",
        "أرز بخاري|خليجي","مضغوط لحم|خليجي","مضغوط دجاج|خليجي","مرقوق|خليجي","قرصان|خليجي"
    };

    private static readonly string[] Dinner =
    {
        "شوربة عدس|مصري","شوربة خضار|مصري","شوربة فراخ|مصري","شوربة لسان عصفور|مصري","حساء حريرة|مغاربي",
        "شوربة شعيرية|شامي","سلطة بلدي عشاء|مصري","تبولة|شامي","فتوش|شامي","سلطة زيتون|شامي",
        "بابا غنوج|شامي","متبل|شامي","حمص عشاء|شامي","فلافل عشاء|مصري","طعمية عشاء|مصري",
        "كفتة صينية|مصري","كباب عشاء|شامي","شيش كباب|خليجي","فراخ مشوية عشاء|مصري","شيش طاووق عشاء|شامي",
        "سمك سنجاري|مصري","سمك فيليه|مصري","جمبري مشوي عشاء|خليجي","كاليماري مقلي|مصري","سردين مشوي|مغاربي",
        "كبة مقلية عشاء|شامي","صفيحة عشاء|شامي","مناقيش عشاء|شامي","فتة دجاج|شامي","فتة حمص باللحمة|شامي",
        "مسخن عشاء|شامي","مقلوبة عشاء|شامي","مجدرة عشاء|شامي","عدس أصفر|مصري","طاجن بطاطس|مصري",
        "مسقعة عشاء|مصري","بامية عشاء|مصري","ملوخية خفيفة|مصري","أرز بشعرية عشاء|مصري","مكرونة بالصلصة|مصري",
        "نودلز بالخضار|خليجي","ساندويتش كبدة|مصري","ساندويتش سجق|مصري","ساندويتش شاورما|شامي","ساندويتش فلافل|مصري",
        "سمبوسة لحمة|خليجي","سمبوسة خضار|خليجي","رقائق جبن|شامي","بيتزا شرقية|شامي","معجنات سبانخ|شامي",
        "فطاير جبنة|شامي","فطاير زعتر|شامي","شوربة فريك|مصري","شوربة كريمة فراخ|مصري","حساء هاريرة خفيف|مغاربي",
        "كسكسي عشاء|مغاربي","طاجين خضار|مغاربي","شخشوخة خفيفة|مغاربي","دجاج مشوي عشاء|مغاربي","سلطة تونسية|مغاربي",
        "سلطة مشوية|مغاربي","بريك تونسي|مغاربي","شكشوكة عشاء|مغاربي","بيض عشاء|مصري","عجة عشاء|شامي",
        "لبنة عشاء|شامي","جبنة مشوية عشاء|خليجي","زيتون ولبنة|شامي","خبز بالثوم|شامي","حمص باللحمة|شامي",
        "يخنة بامية|مصري","يخنة فاصوليا|مصري","يخنة بسلة|مصري","يخنة بطاطس|مصري","كفتة بالطحينة|شامي",
        "كباب هندي خليجي|خليجي","تيكة دجاج|خليجي","توست شيش|خليجي","مشاوي مشكلة|خليجي","ريش ضأن|خليجي",
        "أوزي صغير|خليجي","محشي صغير|مصري","ورق عنب عشاء|شامي","كوسا محشية|شامي","فلفل محشي|مصري",
        "كنافة نابلسية|شامي","قطايف|مصري","بقلاوة فستق|شامي","بسبوسة|مصري","لقيمات|خليجي",
        "مهلبية|شامي","أرز بلبن|مصري","أم علي|مصري","بلح الشام|شامي","غريبة|شامي",
        "كعك العيد الصغير|مصري","سلطة فواكه|مصري","مهلبية بالفستق|خليجي","أم علي بالمكسرات|مصري","عصيدة|خليجي"
    };

    private static readonly string[] Desserts =
    {
        "كنافة نابلسية|شامي","كنافة بالقشطة|شامي","كنافة بالفستق|شامي","كنافة بالجوز|شامي","كنافة مبرومة|شامي","قطايف عصافيري|مصري","قطايف بالجوز|شامي","قطايف بالقشطة|شامي","قطايف مقلية|مصري","قطايف بالفستق|شامي",
        "بقلاوة فستق|شامي","بقلاوة جوز|شامي","بقلاوة كاجو|شامي","بقلاوة أصابع|شامي","بقلاوة مشكلة|شامي","بسبوسة|مصري","بسبوسة بالقشطة|مصري","بسبوسة باللوز|مصري","بسبوسة جوز الهند|مصري","هريسة|شامي",
        "نمورة|شامي","نمورة بالقشطة|شامي","بلح الشام|مصري","زنود الست|شامي","أصابع زينب|مصري","عيش السرايا|شامي","ليالي بيروت|شامي","مهلبية|مصري","مهلبية بالفستق|خليجي","مهلبية بالورد|شامي",
        "أرز بلبن|مصري","أرز بلبن بالفستق|مصري","أم علي|مصري","أم علي بالمكسرات|مصري","لقيمات|خليجي","لقيمات بالسمسم|خليجي","غريبة|شامي","غريبة بالفستق|شامي","كعك العيد|مصري","كعك بالتمر|خليجي",
        "معمول تمر|شامي","معمول جوز|شامي","معمول فستق|شامي","برازق|شامي","حلاوة الجبن|شامي","حلاوة طحينية|مصري","رز بحليب|مغاربي","عصيدة حلوة|خليجي","عوامة|مغاربي","زلابية|مصري",
        "مشبك|شامي","حلوى الشعيرية|مصري","تمر محشي لوز|مغاربي","تمر محشي جوز|خليجي","تمر بالطحينة|مصري","خشاف|مصري","جزرية|شامي","كيكة التمر|خليجي","كيكة السميد|مصري","بسكويت السميد|مصري",
        "عيون الغزال|مغاربي","كعب الغزال|مغاربي","قراشل|مغاربي","فقاص|مغاربي","مقروط التمر|مغاربي","مقروط اللوز|مغاربي","كعك الورقة|مغاربي","سفوف|مغاربي","سلو|مغاربي","زميتة|مغاربي",
        "محنشة|مغاربي","قرن غزال|مغاربي","خبيصة|خليجي","سلطة فواكه|مصري","قدرة قادر|مغاربي","مبرومة فستق|شامي","عش البلبل|شامي","كنافة مانجو|مصري","كنافة بالكريمة|شامي","بسبوسة الصينية|مصري",
        "حلى التمر|خليجي","حلى الجزر|خليجي","غريبة سميد|مصري","غريبة مغربية|مغاربي","رغايف بالعسل|مغاربي","ملوي بالعسل|مغاربي","بطبوط بالعسل|مغاربي","فطير بالعسل|مصري","رقاق بالعسل|مصري","مسمن بالعسل|مغاربي",
        "بغرير بالعسل|مغاربي","تشيز السميد|مصري","مهلبية قمر الدين|شامي","جلاب حلو|شامي","حلاوة الشعر|مصري","كنافة عصملية|شامي","قطايف بالبندق|شامي","بقلاوة بالقطر|شامي","أم علي بالزبيب|مصري","مشكلة مكسرات|خليجي"
    };

    private static readonly string[] Drinks =
    {
        "شاي أحمر|مصري","شاي بالنعناع|مغاربي","شاي كركديه|مصري","شاي بالحليب|خليجي","شاي بالهيل|خليجي","شاي بالقرفة|شامي","شاي بالزنجبيل|مصري","شاي بالزعتر|شامي","شاي مغربي|مغاربي","شاي عدني|خليجي",
        "قهوة تركي|شامي","قهوة عربي|خليجي","قهوة بالهيل|خليجي","قهوة سعودية|خليجي","قهوة بالزعفران|خليجي","قهوة بحليب|مصري","قهوة إماراتية|خليجي","قهوة يمنية|خليجي","قشر قهوة|خليجي","قهوة بيضاء|خليجي",
        "كركديه|مصري","كركديه بالنعناع|مصري","تمر هندي|مصري","تمر هندي باللبن|مصري","عرقسوس|مصري","عرقسوس مثلج|مصري","سحلب|شامي","سحلب بالمكسرات|شامي","سحلب بالقرفة|شامي","سوبيا|مصري",
        "سوبيا باللبن|مصري","خروب|شامي","خروب بالحليب|شامي","قمر الدين|شامي","قمر الدين باللبن|شامي","عصير ليمون|مصري","عصير ليمون بالنعناع|شامي","عصير برتقال|مصري","عصير مانجو|مصري","عصير جوافة|مصري",
        "عصير فراولة|مصري","عصير رمان|شامي","عصير قصب|مصري","عصير جزر|مصري","عصير تفاح|شامي","عصير موز باللبن|مصري","عصير توت|شامي","عصير أناناس|مصري","عصير بطيخ|مصري","عصير شمام|خليجي",
        "عصير خوخ|شامي","عصير مشمش|شامي","عصير بلح|خليجي","عصير أفوكادو|مصري","لبن رايب|مصري","عيران|شامي","عيران بالنعناع|شامي","عيران بالخيار|شامي","لبن بالتمر|خليجي","لبن بالزعفران|خليجي",
        "شاي كرك|خليجي","كرك بالزعفران|خليجي","شاي أحمر إماراتي|خليجي","شاي كويتي|خليجي","شاي بحريني بالهيل|خليجي","أتاي|مغاربي","زهورات|شامي","بابونج|شامي","مريمية|مغاربي","نعناع مغلي|مغاربي",
        "ينسون|مصري","كراوية|مصري","حلبة|مصري","قرفة باللبن|شامي","زنجبيل بالعسل|مصري","كاكاو بالحليب|مصري","هوت شوكليت|مصري","شراب الورد|شامي","شراب الرمان|شامي","شراب التوت|شامي",
        "جلّاب|شامي","جلّاب بالفستق|شامي","مشروب التمر بالحليب|خليجي","عصير قصب بالليمون|مصري","عصير جوافة باللبن|مصري","عصير برتقال بالجزر|مصري","عصير رمان بالنعناع|شامي","كوكتيل فواكه|مصري","ليمون بالنعناع|شامي","شاي مثلج|مصري",
        "قهوة مثلجة|خليجي","آيس كركديه|مصري","آيس تمر هندي|مصري","آيس ليمون|مصري","ميلك شيك تمر|خليجي","ميلك شيك مانجو|مصري","ميلك شيك فراولة|مصري","سحلب سادة|شامي","خروب مثلج|شامي","قمر الدين الساخن|شامي"
    };

    private static IReadOnlyList<PantryRow> Pantry() => new PantryRow[]
    {
        new("فول", "فول", "جرام", "البقالة", "fava beans", "بقول", "جاف", 50),
        new("حمص", "حمص", "جرام", "البقالة", "chickpeas", "بقول", "جاف", 50),
        new("عدس", "عدس", "جرام", "البقالة", "lentils", "بقول", "جاف", 50),
        new("طحينة", "طحينة", "جرام", "البقالة", "tahini", "بهارات", "جاف", 20),
        new("زيت", "زيت", "مل", "البقالة", "olive oil bottle", "بهارات", "جاف", 15),
        new("ليمون", "ليمون", "حبة", "الخضارجي", "lemon fruit", "خضار", "طازج", 1),
        new("كمون", "كمون", "جرام", "البقالة", "cumin spice", "بهارات", "جاف", 5),
        new("طماطم", "طماطم", "جرام", "الخضارجي", "tomatoes", "خضار", "طازج", 50),
        new("بصل", "بصل", "جرام", "الخضارجي", "onions", "خضار", "طازج", 50),
        new("ثوم", "ثوم", "جرام", "الخضارجي", "garlic", "خضار", "طازج", 10),
        new("بيض", "بيض", "حبة", "البقالة", "eggs", "ألبان", "طازج", 1),
        new("جبنة", "جبنة بيضاء", "جرام", "البقالة", "white cheese", "ألبان", "طازج", 30),
        new("لبن", "لبن", "مل", "البقالة", "milk", "ألبان", "طازج", 100),
        new("زبادي", "زبادي", "جرام", "البقالة", "yogurt", "ألبان", "طازج", 50),
        new("عسل", "عسل", "جرام", "البقالة", "honey", "بهارات", "جاف", 20),
        new("سمن", "سمن", "جرام", "البقالة", "ghee", "ألبان", "جاف", 15),
        new("خبز", "خبز", "حبة", "الفرن", "arabic bread", "مخبوزات", "طازج", 1),
        new("بقدونس", "بقدونس", "جرام", "الخضارجي", "parsley", "خضار", "طازج", 10),
        new("كزبرة", "كزبرة", "جرام", "الخضارجي", "coriander leaves", "خضار", "طازج", 10),
        new("نعناع", "نعناع", "جرام", "الخضارجي", "mint leaves", "خضار", "طازج", 5),
        new("بطاطس", "بطاطس", "جرام", "الخضارجي", "potatoes", "خضار", "طازج", 80),
        new("باذنجان", "باذنجان", "جرام", "الخضارجي", "eggplant", "خضار", "طازج", 80),
        new("كوسة", "كوسة", "جرام", "الخضارجي", "zucchini", "خضار", "طازج", 80),
        new("فلفل", "فلفل رومي", "جرام", "الخضارجي", "bell pepper", "خضار", "طازج", 40),
        new("ملوخية", "ملوخية", "جرام", "الخضارجي", "molokhia leaves", "خضار", "طازج", 50),
        new("بامية", "بامية", "جرام", "الخضارجي", "okra", "خضار", "طازج", 80),
        new("أرز", "أرز", "جرام", "البقالة", "rice grains", "حبوب", "جاف", 50),
        new("مكرونة", "مكرونة", "جرام", "البقالة", "pasta", "حبوب", "جاف", 50),
        new("لحمة", "لحمة مفرومة", "جرام", "الجزار", "ground beef", "لحوم", "طازج", 100),
        new("لحم غنم", "لحم غنم", "جرام", "الجزار", "lamb meat", "لحوم", "طازج", 100),
        new("فراخ", "فراخ", "جرام", "سوق الفراخ", "chicken", "لحوم", "طازج", 100),
        new("سمك", "سمك", "جرام", "سوق السمك", "fish fillet", "لحوم", "طازج", 150),
        new("جمبري", "جمبري", "جرام", "سوق السمك", "shrimp", "لحوم", "طازج", 100),
        new("كبدة", "كبدة", "جرام", "الجزار", "liver", "لحوم", "طازج", 100),
        new("زيتون", "زيتون", "جرام", "البقالة", "olives", "خضار", "معلب", 30),
        new("خيار", "خيار", "جرام", "الخضارجي", "cucumber", "خضار", "طازج", 50),
        new("سمسم", "سمسم", "جرام", "البقالة", "sesame", "بهارات", "جاف", 10),
        new("تمر", "تمر", "جرام", "البقالة", "dates fruit", "حبوب", "جاف", 30),
        new("لبنة", "لبنة", "جرام", "البقالة", "labneh", "ألبان", "طازج", 40),
        new("دقيق", "دقيق", "جرام", "البقالة", "flour", "مخبوزات", "جاف", 50),
        new("سكر", "سكر", "جرام", "البقالة", "sugar", "بهارات", "جاف", 20),
        new("ملح", "ملح", "جرام", "البقالة", "salt", "بهارات", "جاف", 2),
        new("فلفل أسود", "فلفل أسود", "جرام", "البقالة", "black pepper", "بهارات", "جاف", 2),
        new("بهار", "بهار مشكل", "جرام", "البقالة", "mixed spices", "بهارات", "جاف", 5),
        new("صلصة", "صلصة طماطم", "جرام", "البقالة", "tomato sauce", "خضار", "معلب", 30),
        new("برغل", "برغل", "جرام", "البقالة", "bulgur", "حبوب", "جاف", 40),
        new("كسكسي", "كسكسي", "جرام", "البقالة", "couscous", "حبوب", "جاف", 50),
        new("قمح", "قمح", "جرام", "البقالة", "wheat berries", "حبوب", "جاف", 40),
        new("خضار", "خضار مشكلة", "جرام", "الخضارجي", "mixed vegetables", "خضار", "طازج", 80),
        new("جزر", "جزر", "جرام", "الخضارجي", "carrots", "خضار", "طازج", 40),
        new("زعتر", "زعتر", "جرام", "البقالة", "zaatar", "بهارات", "جاف", 5),
        new("سماق", "سماق", "جرام", "البقالة", "sumac", "بهارات", "جاف", 3),
        new("خس", "خس", "جرام", "الخضارجي", "lettuce", "خضار", "طازج", 30),
        new("لوز", "لوز", "جرام", "البقالة", "almonds", "حبوب", "جاف", 15),
        new("فستق", "فستق", "جرام", "البقالة", "pistachio", "حبوب", "جاف", 15),
        new("جوز", "جوز", "جرام", "البقالة", "walnuts", "حبوب", "جاف", 15),
        new("مكسرات", "مكسرات", "جرام", "البقالة", "mixed nuts", "حبوب", "جاف", 15),
        new("قطر", "قطر", "مل", "البقالة", "sugar syrup", "بهارات", "جاف", 20),
        new("سميد", "سميد", "جرام", "البقالة", "semolina", "مخبوزات", "جاف", 40),
        new("نشا", "نشا", "جرام", "البقالة", "starch", "مخبوزات", "جاف", 10),
        new("رقاق", "رقاق", "جرام", "الفرن", "phyllo pastry", "مخبوزات", "طازج", 30),
        new("برقوق", "برقوق مجفف", "جرام", "البقالة", "prunes", "حبوب", "جاف", 20),
        new("قهوة", "قهوة", "جرام", "البقالة", "arabic coffee", "بهارات", "جاف", 10),
        new("شاي", "شاي", "جرام", "البقالة", "black tea", "بهارات", "جاف", 5),
        new("كركديه", "كركديه", "جرام", "البقالة", "hibiscus", "بهارات", "جاف", 10),
        new("تمر هندي", "تمر هندي", "جرام", "البقالة", "tamarind", "بهارات", "جاف", 15),
        new("عرقسوس", "عرقسوس", "جرام", "البقالة", "licorice", "بهارات", "جاف", 10),
        new("هيل", "هيل", "جرام", "البقالة", "cardamom", "بهارات", "جاف", 2),
        new("قرفة", "قرفة", "جرام", "البقالة", "cinnamon", "بهارات", "جاف", 2),
        new("خروب", "خروب", "جرام", "البقالة", "carob", "بهارات", "جاف", 15),
        new("قمر الدين", "قمر الدين", "جرام", "البقالة", "apricot leather", "حبوب", "جاف", 20),
        new("جوز هند", "جوز هند", "جرام", "البقالة", "coconut", "حبوب", "جاف", 15),
        new("قشطة", "قشطة", "جرام", "البقالة", "clotted cream", "ألبان", "طازج", 30)
    };

    private sealed record Dish(string Name, string Region, string Meal);
    private sealed record PantryRow(string Key, string Name, string Unit, string Place, string Search, string Category, string Group, decimal Mini);
}
