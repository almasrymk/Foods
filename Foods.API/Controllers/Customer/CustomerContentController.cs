namespace Foods.API.Controllers.Customer
{
    using MediatR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Foods.Domain.Entity.Security;
    using Foods.Application.Customer.Catalog;

    /// <summary>Ingredient picker for the customer's own version of a dish (existing ingredients only).</summary>
    [Authorize(Roles = $"{RoleNames.Customer},{RoleNames.Admin}")]
    [ApiController]
    [Route("customer/contents")]
    public class CustomerContentController(ISender sender) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string? text, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
        {
            return this.From(await sender.Send(new SearchContentsQuery(text, take), cancellationToken));
        }
    }
}
