namespace Foods.API.Controllers.Customer
{
    using MediatR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Foods.Domain.Entity.Security;
    using Foods.Application.Customer.Pricing;

    public sealed record PricesEnabledBody(bool Enabled);
    public sealed record MyPriceBody(decimal Price, decimal Quantity, Guid? UnitId);
    public sealed record PriceCountryBody(Guid? CountryId);

    /// <summary>أسعار المكونات: the customer turns prices on and writes their own ingredient prices.</summary>
    [Authorize(Roles = $"{RoleNames.Customer},{RoleNames.Admin}")]
    [ApiController]
    [Route("customer/prices")]
    public class CustomerPriceController(ISender sender) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> List(string? text, Guid? contentId, CancellationToken cancellationToken)
            => this.From(await sender.Send(new GetMyPricesQuery(text, contentId), cancellationToken));

        /// <summary>The country the customer lives in now (prices and currency); null = their own country.</summary>
        [HttpGet("country")]
        public async Task<IActionResult> Country(CancellationToken cancellationToken)
            => this.From(await sender.Send(new GetPriceCountryQuery(), cancellationToken));

        [HttpPut("country")]
        public async Task<IActionResult> SetCountry([FromBody] PriceCountryBody body, CancellationToken cancellationToken)
            => this.From(await sender.Send(new SetPriceCountryCommand(body?.CountryId), cancellationToken));

        [HttpPut("enabled")]
        public async Task<IActionResult> Enabled([FromBody] PricesEnabledBody body, CancellationToken cancellationToken)
            => this.From(await sender.Send(new SetPricesEnabledCommand(body.Enabled), cancellationToken));

        [HttpPut("{contentId:guid}")]
        public async Task<IActionResult> Save(Guid contentId, [FromBody] MyPriceBody body, CancellationToken cancellationToken)
            => this.From(await sender.Send(new SaveMyPriceCommand(contentId, body.Price, body.Quantity, body.UnitId), cancellationToken));

        [HttpDelete("{contentId:guid}")]
        public async Task<IActionResult> Remove(Guid contentId, CancellationToken cancellationToken)
            => this.From(await sender.Send(new RemoveMyPriceCommand(contentId), cancellationToken));
    }
}
