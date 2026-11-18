namespace Foods.Application.MasterData.Versions
{
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData.Item;
    using Foods.Domain.Entity.MasterData.Content;
    using Foods.Application.Abstraction.Query;
    using Foods.Application.Abstraction.Command;
    using Foods.Application.MasterData.Item.ItemPart;

    // ---------------- ItemVersion: the general recipe and each country's own recipe of a dish ----------------

    public sealed record VersionLine(Guid ContentId, Guid UnitId, decimal Quantity, bool IsOptional, bool IsDefault);
    public sealed record VersionPart(Guid PartItemId, bool IsOptional, bool IsDefault);
    public sealed record VersionStep(int Sort, string? Body);
    public sealed record VersionVideo(int Sort, string? Title, string? Url);

    /// <param name="CountryId">Null = the general recipe (used by every country without its own).</param>
    public sealed record ItemVersionDto(Guid? CountryId, List<VersionLine> Lines, List<VersionPart> Parts, List<VersionStep> Steps, List<VersionVideo> Videos);

    public sealed record GetItemVersionsQuery(Guid ItemId) : IQuery<List<ItemVersionDto>>;

    /// <summary>
    /// Replaces the general recipe and the country recipes of a dish. A linked country (ItemCountry) that is not
    /// in the list goes back to the general recipe. Countries must be linked to the dish first.
    /// </summary>
    public sealed record SaveItemVersionsCommand(Guid ItemId, IReadOnlyList<ItemVersionDto>? Versions) : ICommand<List<ItemVersionDto>>;

    file static class ItemVersions
    {
        public static async Task<List<ItemVersionDto>> Load(IFoodsDbContext db, Guid itemId, CancellationToken ct)
        {
            var own = await db.ItemCountries.Where(e => e.ItemId == itemId && !e.IsDeleted && e.OwnRecipe).Select(e => e.CountryId).Distinct().ToListAsync(ct);
            var lines = await db.ItemContents.Where(e => e.ItemId == itemId && !e.IsDeleted).ToListAsync(ct);
            var parts = await db.ItemParts.Where(e => e.ItemId == itemId && !e.IsDeleted).OrderBy(e => e.Sort).ToListAsync(ct);
            var steps = await db.ItemSteps.Where(e => e.ItemId == itemId && !e.IsDeleted).OrderBy(e => e.Sort).ToListAsync(ct);
            var videos = await db.ItemVideos.Where(e => e.ItemId == itemId && !e.IsDeleted).OrderBy(e => e.Sort).ToListAsync(ct);
            ItemVersionDto Of(Guid? country) => new(
                country,
                lines.Where(e => e.CountryId == country).Select(e => new VersionLine(e.ContentId, e.UnitId, e.Quantity, e.IsOptional, e.IsDefault)).ToList(),
                parts.Where(e => e.CountryId == country).Select(e => new VersionPart(e.PartItemId, e.IsOptional, e.IsDefault)).ToList(),
                steps.Where(e => e.CountryId == country).Select(e => new VersionStep(e.Sort, e.Body)).ToList(),
                videos.Where(e => e.CountryId == country).Select(e => new VersionVideo(e.Sort, e.Title, e.Url)).ToList());
            var result = new List<ItemVersionDto> { Of(null) };
            result.AddRange(own.Select(country => Of(country)));
            return result;
        }
    }

    public sealed class GetItemVersionsQueryHandler(IFoodsDbContext db) : IQueryHandler<GetItemVersionsQuery, List<ItemVersionDto>>
    {
        public async Task<Result<List<ItemVersionDto>>> Handle(GetItemVersionsQuery request, CancellationToken cancellationToken)
        {
            if (!await db.Items.AnyAsync(e => e.Id == request.ItemId && !e.IsDeleted, cancellationToken))
                return new(HttpStatusCode.NotFound, null, new List<string> { "الأكلة غير موجودة" });
            return new(HttpStatusCode.OK, await ItemVersions.Load(db, request.ItemId, cancellationToken), null);
        }
    }

    public sealed class SaveItemVersionsCommandHandler(IFoodsDbContext db) : ICommandHandler<SaveItemVersionsCommand, List<ItemVersionDto>>
    {
        public async Task<Result<List<ItemVersionDto>>> Handle(SaveItemVersionsCommand request, CancellationToken cancellationToken)
        {
            var itemId = request.ItemId;
            if (!await db.Items.AnyAsync(e => e.Id == itemId && !e.IsDeleted, cancellationToken))
                return Bad("الأكلة غير موجودة", HttpStatusCode.NotFound);
            var versions = (request.Versions ?? Array.Empty<ItemVersionDto>()).ToList();
            if (versions.Select(e => e.CountryId).Distinct().Count() != versions.Count)
                return Bad("نسخة البلد متكررة");
            var links = await db.ItemCountries.Where(e => e.ItemId == itemId && !e.IsDeleted).ToListAsync(cancellationToken);
            foreach (var version in versions.Where(e => e.CountryId != null))
                if (!links.Any(l => l.CountryId == version.CountryId))
                    return Bad("ضيف البلد للأكلة الأول قبل ما تعمل لها نسخة");

            // Check everything before changing anything.
            var contentIds = versions.SelectMany(v => v.Lines).Select(e => e.ContentId).Distinct().ToList();
            if (await db.Contents.CountAsync(e => contentIds.Contains(e.Id) && !e.IsDeleted, cancellationToken) != contentIds.Count)
                return Bad("اختار من المكونات الموجودة بس");
            var unitIds = versions.SelectMany(v => v.Lines).Select(e => e.UnitId).Distinct().ToList();
            if (await db.Units.CountAsync(e => unitIds.Contains(e.Id) && !e.IsDeleted, cancellationToken) != unitIds.Count)
                return Bad("الوحدة غير موجودة");
            foreach (var version in versions)
            {
                if (version.Lines.Select(e => e.ContentId).Distinct().Count() != version.Lines.Count)
                    return Bad("المكوّن متكرر في نفس النسخة");
                if (version.Lines.Any(e => e.Quantity < 0))
                    return Bad("الكمية مينفعش تبقى بالسالب");
                var partIds = version.Parts.Select(e => e.PartItemId).Where(e => e != Guid.Empty).ToList();
                if (partIds.Distinct().Count() != partIds.Count) return Bad("الصنف متكرر في نفس النسخة");
                if (partIds.Contains(itemId)) return Bad("الأكلة متتضافش كصنف لنفسها");
                if (await db.Items.CountAsync(e => partIds.Contains(e.Id) && !e.IsDeleted, cancellationToken) != partIds.Count)
                    return Bad("اختار من الأكلات الموجودة بس");
                foreach (var partId in partIds)
                    if (await ItemParts.MakesLoop(db, itemId, partId, cancellationToken))
                        return Bad("في صنف جواه الأكلة دي نفسها، فمينفعش يتضاف");
            }

            // Replace all rows of the dish (general + countries).
            foreach (var row in await db.ItemContents.Where(e => e.ItemId == itemId && !e.IsDeleted).ToListAsync(cancellationToken)) row.IsDeleted = true;
            foreach (var row in await db.ItemParts.Where(e => e.ItemId == itemId && !e.IsDeleted).ToListAsync(cancellationToken)) row.IsDeleted = true;
            foreach (var row in await db.ItemSteps.Where(e => e.ItemId == itemId && !e.IsDeleted).ToListAsync(cancellationToken)) row.IsDeleted = true;
            foreach (var row in await db.ItemVideos.Where(e => e.ItemId == itemId && !e.IsDeleted).ToListAsync(cancellationToken)) row.IsDeleted = true;
            foreach (var link in links)
                link.OwnRecipe = versions.Any(v => v.CountryId == link.CountryId);

            foreach (var version in versions)
            {
                var country = version.CountryId;
                foreach (var line in version.Lines)
                    db.ItemContents.Add(new ItemContent
                    {
                        Id = Guid.NewGuid(), ItemId = itemId, CountryId = country, ContentId = line.ContentId, UnitId = line.UnitId,
                        Quantity = line.Quantity, IsOptional = line.IsOptional, IsDefault = !line.IsOptional || line.IsDefault, IsDeleted = false, Status = 0
                    });
                var sort = 1;
                foreach (var part in version.Parts.Where(e => e.PartItemId != Guid.Empty))
                    db.ItemParts.Add(new ItemPart
                    {
                        Id = Guid.NewGuid(), ItemId = itemId, CountryId = country, PartItemId = part.PartItemId,
                        IsOptional = part.IsOptional, IsDefault = !part.IsOptional || part.IsDefault, Sort = sort++, IsDeleted = false, Status = 0
                    });
                sort = 1;
                foreach (var step in version.Steps.Where(e => !string.IsNullOrWhiteSpace(e.Body)).OrderBy(e => e.Sort))
                    db.ItemSteps.Add(new ItemStep { Id = Guid.NewGuid(), ItemId = itemId, CountryId = country, Sort = sort++, Body = step.Body!.Trim(), IsDeleted = false, Status = 0 });
                sort = 1;
                foreach (var video in version.Videos.Where(e => !string.IsNullOrWhiteSpace(e.Url) || !string.IsNullOrWhiteSpace(e.Title)).OrderBy(e => e.Sort))
                    db.ItemVideos.Add(new ItemVideo { Id = Guid.NewGuid(), ItemId = itemId, CountryId = country, Sort = sort++, Title = video.Title, Url = video.Url, IsDeleted = false, Status = 0 });
            }

            await db.SaveChangesAsync(cancellationToken);
            return new(HttpStatusCode.OK, await ItemVersions.Load(db, itemId, cancellationToken), null);
        }

        private static Result<List<ItemVersionDto>> Bad(string message, HttpStatusCode code = HttpStatusCode.BadRequest)
            => new(code, null, new List<string> { message });
    }

    // ---------------- ContentCountry: an ingredient's name, description and price in each country ----------------

    public sealed record ContentCountryRow(Guid CountryId, string? Name, string? Description, decimal Price, decimal PriceQuantity, Guid? PriceUnitId);

    public sealed record GetContentCountriesQuery(Guid ContentId) : IQuery<List<ContentCountryRow>>;
    public sealed record SaveContentCountriesCommand(Guid ContentId, IReadOnlyList<ContentCountryRow>? Rows) : ICommand<List<ContentCountryRow>>;

    file static class ContentCountries
    {
        public static async Task<List<ContentCountryRow>> Load(IFoodsDbContext db, Guid contentId, CancellationToken ct)
            => await db.ContentCountries.Where(e => e.ContentId == contentId && !e.IsDeleted)
                .Select(e => new ContentCountryRow(e.CountryId, e.Name, e.Description, e.Price, e.PriceQuantity, e.PriceUnitId))
                .ToListAsync(ct);
    }

    public sealed class GetContentCountriesQueryHandler(IFoodsDbContext db) : IQueryHandler<GetContentCountriesQuery, List<ContentCountryRow>>
    {
        public async Task<Result<List<ContentCountryRow>>> Handle(GetContentCountriesQuery request, CancellationToken cancellationToken)
            => new(HttpStatusCode.OK, await ContentCountries.Load(db, request.ContentId, cancellationToken), null);
    }

    public sealed class SaveContentCountriesCommandHandler(IFoodsDbContext db) : ICommandHandler<SaveContentCountriesCommand, List<ContentCountryRow>>
    {
        public async Task<Result<List<ContentCountryRow>>> Handle(SaveContentCountriesCommand request, CancellationToken cancellationToken)
        {
            if (!await db.Contents.AnyAsync(e => e.Id == request.ContentId && !e.IsDeleted, cancellationToken))
                return new(HttpStatusCode.NotFound, null, new List<string> { "المكوّن غير موجود" });
            var rows = (request.Rows ?? Array.Empty<ContentCountryRow>()).Where(e => e.CountryId != Guid.Empty).ToList();
            if (rows.Select(e => e.CountryId).Distinct().Count() != rows.Count)
                return new(HttpStatusCode.BadRequest, null, new List<string> { "البلد متكررة على المكوّن" });
            if (rows.Any(e => e.Price < 0))
                return new(HttpStatusCode.BadRequest, null, new List<string> { "السعر مينفعش يبقى بالسالب" });
            var countryIds = rows.Select(e => e.CountryId).ToList();
            if (await db.Countries.CountAsync(e => countryIds.Contains(e.Id) && !e.IsDeleted, cancellationToken) != countryIds.Count)
                return new(HttpStatusCode.BadRequest, null, new List<string> { "البلد غير موجودة" });

            foreach (var old in await db.ContentCountries.Where(e => e.ContentId == request.ContentId && !e.IsDeleted).ToListAsync(cancellationToken))
                old.IsDeleted = true;
            foreach (var row in rows)
                db.ContentCountries.Add(new ContentCountry
                {
                    Id = Guid.NewGuid(), ContentId = request.ContentId, CountryId = row.CountryId,
                    Name = string.IsNullOrWhiteSpace(row.Name) ? null : row.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(row.Description) ? null : row.Description.Trim(),
                    Price = row.Price, PriceQuantity = row.PriceQuantity > 0 ? row.PriceQuantity : 1, PriceUnitId = row.PriceUnitId,
                    IsDeleted = false, Status = 0
                });
            await db.SaveChangesAsync(cancellationToken);
            return new(HttpStatusCode.OK, await ContentCountries.Load(db, request.ContentId, cancellationToken), null);
        }
    }
}
