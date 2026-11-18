namespace Foods.API.Controllers.MasterData.Versions
{
    using MediatR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Foods.Application.MasterData.Versions;

    /// <summary>A dish's general recipe and each country's own recipe (ingredients, parts, steps, videos).</summary>
    [Authorize(Roles = Foods.Domain.Entity.Security.RoleNames.Admin)]
    [ApiController]
    [Route("[controller]/[action]")]
    public class ItemVersionController(ISender sender) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetList(Guid ItemId, CancellationToken cancellationToken)
            => this.From(await sender.Send(new GetItemVersionsQuery(ItemId), cancellationToken));

        [HttpPut]
        public async Task<IActionResult> Save([FromBody] SaveItemVersionsCommand command, CancellationToken cancellationToken)
            => this.From(await sender.Send(command, cancellationToken));
    }

    /// <summary>An ingredient in each country: its name, description and price.</summary>
    [Authorize(Roles = Foods.Domain.Entity.Security.RoleNames.Admin)]
    [ApiController]
    [Route("[controller]/[action]")]
    public class ContentCountryController(ISender sender) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetList(Guid ContentId, CancellationToken cancellationToken)
            => this.From(await sender.Send(new GetContentCountriesQuery(ContentId), cancellationToken));

        [HttpPut]
        public async Task<IActionResult> Save([FromBody] SaveContentCountriesCommand command, CancellationToken cancellationToken)
            => this.From(await sender.Send(command, cancellationToken));
    }
}
