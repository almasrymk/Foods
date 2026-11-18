namespace Foods.API.Controllers.MasterData
{
    using MediatR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Foods.Application.MasterData.Album;

    /// <summary>Photo albums of dishes (kind = item) and ingredients (kind = content).</summary>
    [Authorize(Roles = Foods.Domain.Entity.Security.RoleNames.Admin)]
    [ApiController]
    [Route("Album")]
    public class AlbumController(ISender sender) : ControllerBase
    {
        public sealed record AddImagesBody(IReadOnlyList<string>? Paths);
        public sealed record ReorderBody(IReadOnlyList<Guid>? Ids);

        private static bool TryKind(string kind, out AlbumKind value)
        {
            value = AlbumKind.Item;
            if (string.Equals(kind, "item", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(kind, "content", StringComparison.OrdinalIgnoreCase)) { value = AlbumKind.Content; return true; }
            return false;
        }

        [HttpGet("{kind}/{ownerId:guid}")]
        public async Task<IActionResult> Get(string kind, Guid ownerId, CancellationToken cancellationToken)
        {
            if (!TryKind(kind, out var k)) return NotFound();
            return this.From(await sender.Send(new GetAlbumQuery(k, ownerId), cancellationToken));
        }

        [HttpPost("{kind}/{ownerId:guid}")]
        public async Task<IActionResult> Add(string kind, Guid ownerId, [FromBody] AddImagesBody body, CancellationToken cancellationToken)
        {
            if (!TryKind(kind, out var k)) return NotFound();
            return this.From(await sender.Send(new AddAlbumImagesCommand(k, ownerId, body?.Paths), cancellationToken));
        }

        [HttpPut("{kind}/{ownerId:guid}/order")]
        public async Task<IActionResult> Reorder(string kind, Guid ownerId, [FromBody] ReorderBody body, CancellationToken cancellationToken)
        {
            if (!TryKind(kind, out var k)) return NotFound();
            return this.From(await sender.Send(new ReorderAlbumCommand(k, ownerId, body?.Ids), cancellationToken));
        }

        [HttpDelete("{kind}/image/{imageId:guid}")]
        public async Task<IActionResult> Delete(string kind, Guid imageId, CancellationToken cancellationToken)
        {
            if (!TryKind(kind, out var k)) return NotFound();
            return this.From(await sender.Send(new DeleteAlbumImageCommand(k, imageId), cancellationToken));
        }
    }
}
