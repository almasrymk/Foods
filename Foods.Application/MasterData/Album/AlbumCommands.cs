namespace Foods.Application.MasterData.Album
{
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData.Item;
    using Foods.Domain.Entity.MasterData.Content;
    using Foods.Application.Abstraction.Query;
    using Foods.Application.Abstraction.Command;

    /// <summary>Album owner: a dish (Item) or an ingredient (Content).</summary>
    public enum AlbumKind { Item, Content }

    public sealed record AlbumImageDto(Guid Id, string? ImagePath, int Sort);

    public sealed record GetAlbumQuery(AlbumKind Kind, Guid OwnerId) : IQuery<List<AlbumImageDto>>;
    public sealed record AddAlbumImagesCommand(AlbumKind Kind, Guid OwnerId, IReadOnlyList<string>? Paths) : ICommand<List<AlbumImageDto>>;
    public sealed record DeleteAlbumImageCommand(AlbumKind Kind, Guid ImageId) : ICommand<Guid>;
    public sealed record ReorderAlbumCommand(AlbumKind Kind, Guid OwnerId, IReadOnlyList<Guid>? Ids) : ICommand<List<AlbumImageDto>>;

    file static class Album
    {
        public static readonly List<string> NotFound = new() { "العنصر غير موجود" };

        public static Task<bool> OwnerExists(IFoodsDbContext db, AlbumKind kind, Guid ownerId, CancellationToken ct)
            => kind == AlbumKind.Item
                ? db.Items.AnyAsync(e => e.Id == ownerId && !e.IsDeleted, ct)
                : db.Contents.AnyAsync(e => e.Id == ownerId && !e.IsDeleted, ct);

        public static Task<List<AlbumImageDto>> List(IFoodsDbContext db, AlbumKind kind, Guid ownerId, CancellationToken ct)
            => kind == AlbumKind.Item
                ? db.ItemImages.Where(e => e.ItemId == ownerId && !e.IsDeleted).OrderBy(e => e.Sort)
                    .Select(e => new AlbumImageDto(e.Id, e.ImagePath, e.Sort)).ToListAsync(ct)
                : db.ContentImages.Where(e => e.ContentId == ownerId && !e.IsDeleted).OrderBy(e => e.Sort)
                    .Select(e => new AlbumImageDto(e.Id, e.ImagePath, e.Sort)).ToListAsync(ct);

        /// <summary>Only files saved by the upload endpoint (files/image) are accepted.</summary>
        public static bool ValidPath(string? path)
            => !string.IsNullOrWhiteSpace(path)
               && path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
               && !path.Contains("..")
               && path.Length <= 400;
    }

    public sealed class GetAlbumQueryHandler(IFoodsDbContext db) : IQueryHandler<GetAlbumQuery, List<AlbumImageDto>>
    {
        public async Task<Result<List<AlbumImageDto>>> Handle(GetAlbumQuery request, CancellationToken cancellationToken)
        {
            if (!await Album.OwnerExists(db, request.Kind, request.OwnerId, cancellationToken))
                return new Result<List<AlbumImageDto>>(HttpStatusCode.NotFound, null, Album.NotFound);
            return new Result<List<AlbumImageDto>>(HttpStatusCode.OK, await Album.List(db, request.Kind, request.OwnerId, cancellationToken), null);
        }
    }

    public sealed class AddAlbumImagesCommandHandler(IFoodsDbContext db) : ICommandHandler<AddAlbumImagesCommand, List<AlbumImageDto>>
    {
        public async Task<Result<List<AlbumImageDto>>> Handle(AddAlbumImagesCommand request, CancellationToken cancellationToken)
        {
            if (!await Album.OwnerExists(db, request.Kind, request.OwnerId, cancellationToken))
                return new Result<List<AlbumImageDto>>(HttpStatusCode.NotFound, null, Album.NotFound);

            var paths = (request.Paths ?? Array.Empty<string>()).Select(e => e?.Trim()).ToList();
            if (paths.Count == 0 || paths.Any(e => !Album.ValidPath(e)))
                return new Result<List<AlbumImageDto>>(HttpStatusCode.BadRequest, null, new List<string> { "ارفع الصور الأول" });

            var existing = await Album.List(db, request.Kind, request.OwnerId, cancellationToken);
            var sort = existing.Count == 0 ? 0 : existing.Max(e => e.Sort) + 1;
            foreach (var path in paths)
            {
                if (request.Kind == AlbumKind.Item)
                    db.ItemImages.Add(new ItemImage { Id = Guid.NewGuid(), ItemId = request.OwnerId, ImagePath = path, Sort = sort++, IsDeleted = false, Status = 0 });
                else
                    db.ContentImages.Add(new ContentImage { Id = Guid.NewGuid(), ContentId = request.OwnerId, ImagePath = path, Sort = sort++, IsDeleted = false, Status = 0 });
            }
            await db.SaveChangesAsync(cancellationToken);
            return new Result<List<AlbumImageDto>>(HttpStatusCode.OK, await Album.List(db, request.Kind, request.OwnerId, cancellationToken), null);
        }
    }

    public sealed class DeleteAlbumImageCommandHandler(IFoodsDbContext db) : ICommandHandler<DeleteAlbumImageCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(DeleteAlbumImageCommand request, CancellationToken cancellationToken)
        {
            BaseEntity? image = request.Kind == AlbumKind.Item
                ? await db.ItemImages.FirstOrDefaultAsync(e => e.Id == request.ImageId && !e.IsDeleted, cancellationToken)
                : await db.ContentImages.FirstOrDefaultAsync(e => e.Id == request.ImageId && !e.IsDeleted, cancellationToken);
            if (image == null)
                return new Result<Guid>(HttpStatusCode.NotFound, Guid.Empty, new List<string> { "الصورة غير موجودة" });

            image.IsDeleted = true;
            await db.SaveChangesAsync(cancellationToken);
            return new Result<Guid>(HttpStatusCode.OK, request.ImageId, null);
        }
    }

    public sealed class ReorderAlbumCommandHandler(IFoodsDbContext db) : ICommandHandler<ReorderAlbumCommand, List<AlbumImageDto>>
    {
        public async Task<Result<List<AlbumImageDto>>> Handle(ReorderAlbumCommand request, CancellationToken cancellationToken)
        {
            if (!await Album.OwnerExists(db, request.Kind, request.OwnerId, cancellationToken))
                return new Result<List<AlbumImageDto>>(HttpStatusCode.NotFound, null, Album.NotFound);

            var order = (request.Ids ?? Array.Empty<Guid>()).ToList();
            if (request.Kind == AlbumKind.Item)
            {
                var images = await db.ItemImages.Where(e => e.ItemId == request.OwnerId && !e.IsDeleted).ToListAsync(cancellationToken);
                Apply(images, e => e.Id, e => e.Sort, (e, s) => e.Sort = s, order);
            }
            else
            {
                var images = await db.ContentImages.Where(e => e.ContentId == request.OwnerId && !e.IsDeleted).ToListAsync(cancellationToken);
                Apply(images, e => e.Id, e => e.Sort, (e, s) => e.Sort = s, order);
            }
            await db.SaveChangesAsync(cancellationToken);
            return new Result<List<AlbumImageDto>>(HttpStatusCode.OK, await Album.List(db, request.Kind, request.OwnerId, cancellationToken), null);
        }

        /// <summary>Listed ids first, in the given order; images not listed keep their order after them.</summary>
        private static void Apply<T>(List<T> images, Func<T, Guid> id, Func<T, int> sort, Action<T, int> setSort, List<Guid> order)
        {
            var ranked = images
                .OrderBy(e => { var i = order.IndexOf(id(e)); return i < 0 ? int.MaxValue : i; })
                .ThenBy(sort)
                .ToList();
            for (var i = 0; i < ranked.Count; i++) setSort(ranked[i], i);
        }
    }
}
