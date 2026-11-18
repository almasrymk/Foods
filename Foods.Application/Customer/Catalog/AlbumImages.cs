namespace Foods.Application.Customer.Catalog
{
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Abstraction;

    /// <summary>
    /// Photo list of dishes / ingredients: the main ImagePath first, then the album by Sort, without duplicates.
    /// Loads the albums of a whole page in one query.
    /// </summary>
    public static class AlbumImages
    {
        public static async Task<Dictionary<Guid, List<string>>> ForItems(IFoodsDbContext db, IEnumerable<(Guid Id, string? ImagePath)> owners, CancellationToken cancellationToken)
        {
            var list = owners.ToList();
            var ids = list.Select(e => e.Id).Distinct().ToList();
            var album = new List<(Guid OwnerId, string? Path, int Sort)>();
            if (ids.Count > 0)
            {
                var rows = await db.ItemImages
                    .Where(e => ids.Contains(e.ItemId) && !e.IsDeleted)
                    .Select(e => new { e.ItemId, e.ImagePath, e.Sort })
                    .ToListAsync(cancellationToken);
                album = rows.Select(e => (e.ItemId, e.ImagePath, e.Sort)).ToList();
            }
            return Merge(list, album);
        }

        public static async Task<Dictionary<Guid, List<string>>> ForContents(IFoodsDbContext db, IEnumerable<(Guid Id, string? ImagePath)> owners, CancellationToken cancellationToken)
        {
            var list = owners.ToList();
            var ids = list.Select(e => e.Id).Distinct().ToList();
            var album = new List<(Guid OwnerId, string? Path, int Sort)>();
            if (ids.Count > 0)
            {
                var rows = await db.ContentImages
                    .Where(e => ids.Contains(e.ContentId) && !e.IsDeleted)
                    .Select(e => new { e.ContentId, e.ImagePath, e.Sort })
                    .ToListAsync(cancellationToken);
                album = rows.Select(e => (e.ContentId, e.ImagePath, e.Sort)).ToList();
            }
            return Merge(list, album);
        }

        /// <summary>The photos of one owner, or just its main image when it was not loaded.</summary>
        public static List<string> Of(Dictionary<Guid, List<string>> map, Guid id, string? fallback)
            => map.TryGetValue(id, out var paths)
                ? paths
                : (string.IsNullOrWhiteSpace(fallback) ? new List<string>() : new List<string> { fallback });

        private static Dictionary<Guid, List<string>> Merge(List<(Guid Id, string? ImagePath)> owners, List<(Guid OwnerId, string? Path, int Sort)> album)
        {
            var byOwner = album.ToLookup(e => e.OwnerId);
            var result = new Dictionary<Guid, List<string>>();
            foreach (var owner in owners)
            {
                if (result.ContainsKey(owner.Id)) continue;
                var paths = new List<string>();
                void Add(string? path)
                {
                    if (!string.IsNullOrWhiteSpace(path) && !paths.Contains(path, StringComparer.OrdinalIgnoreCase))
                        paths.Add(path);
                }
                Add(owner.ImagePath);
                foreach (var image in byOwner[owner.Id].OrderBy(e => e.Sort)) Add(image.Path);
                result[owner.Id] = paths;
            }
            return result;
        }
    }
}
