namespace Foods.API.Controllers.MasterData.Item
{
    using MediatR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Foods.Application.MasterData.Item.ItemRecipe;

    [Authorize(Roles = Foods.Domain.Entity.Security.RoleNames.Admin)]
    [ApiController]
    [Route("[controller]/[action]")]
    public class ItemRecipeController(ISender sender) : ControllerBase
    {
        [HttpPut]
        public async Task<IActionResult> Save([FromBody] SaveItemRecipeCommand command, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(command, cancellationToken));
        }
    }
}
