namespace Foods.Application.MasterData.Item.ItemRecipe
{
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Foods.Domain.Shared;
    using Foods.Domain.Abstraction;
    using Foods.Domain.Entity.MasterData.Item;
    using Foods.Application.Abstraction.Command;

    public sealed record ItemStepInput(int Sort, string? Body);
    public sealed record ItemVideoInput(int Sort, string? Title, string? Url);
    public sealed record SaveItemRecipeCommand(Guid ItemId, IReadOnlyList<ItemStepInput>? Steps, IReadOnlyList<ItemVideoInput>? Videos) : ICommand<Guid>;

    public sealed class SaveItemRecipeCommandHandler(IFoodsDbContext db) : ICommandHandler<SaveItemRecipeCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(SaveItemRecipeCommand request, CancellationToken cancellationToken)
        {
            var item = await db.Items.FirstOrDefaultAsync(e => e.Id == request.ItemId && !e.IsDeleted, cancellationToken);
            if (item == null)
                return new Result<Guid>(HttpStatusCode.NotFound, Guid.Empty, new List<string> { "الأكلة غير موجودة" });

            var steps = await db.ItemSteps.Where(e => e.ItemId == request.ItemId && !e.IsDeleted && e.CountryId == null).ToListAsync(cancellationToken);
            var videos = await db.ItemVideos.Where(e => e.ItemId == request.ItemId && !e.IsDeleted && e.CountryId == null).ToListAsync(cancellationToken);
            foreach (var step in steps) step.IsDeleted = true;
            foreach (var video in videos) video.IsDeleted = true;

            foreach (var step in request.Steps ?? Array.Empty<ItemStepInput>())
            {
                db.ItemSteps.Add(new ItemStep
                {
                    Id = Guid.NewGuid(),
                    ItemId = request.ItemId,
                    Sort = step.Sort,
                    Body = step.Body,
                    IsDeleted = false,
                    Status = 0
                });
            }

            foreach (var video in request.Videos ?? Array.Empty<ItemVideoInput>())
            {
                db.ItemVideos.Add(new ItemVideo
                {
                    Id = Guid.NewGuid(),
                    ItemId = request.ItemId,
                    Sort = video.Sort,
                    Title = video.Title,
                    Url = video.Url,
                    IsDeleted = false,
                    Status = 0
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            return new Result<Guid>(HttpStatusCode.OK, request.ItemId, null);
        }
    }
}
