namespace Foods.API.Controllers.MasterData.Item
{
    using MediatR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Foods.Application.MasterData.Item.ItemPart;

    /// <summary>Parts (أصناف) of a dish: other dishes that are required or optional inside it.</summary>
    [Authorize(Roles = Foods.Domain.Entity.Security.RoleNames.Admin)]
    [ApiController]
    [Route("[controller]/[action]")]
    public class ItemPartController(ISender sender) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetList(Guid ItemId, CancellationToken cancellationToken)
            => this.From(await sender.Send(new GetItemPartsQuery(ItemId), cancellationToken));

        [HttpPut]
        public async Task<IActionResult> Save([FromBody] SaveItemPartsCommand command, CancellationToken cancellationToken)
            => this.From(await sender.Send(command, cancellationToken));
    }
}
