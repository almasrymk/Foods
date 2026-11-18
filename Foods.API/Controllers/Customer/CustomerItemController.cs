namespace Foods.API.Controllers.Customer
{
    using MediatR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Foods.Domain.Entity.Security;
    using Foods.Application.Customer.Catalog;
    using Foods.Application.Customer.Favorites;

    [Authorize(Roles = $"{RoleNames.Customer},{RoleNames.Admin}")]
    [ApiController]
    [Route("customer/items")]
    public class CustomerItemController(ISender sender) : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id, [FromQuery] Guid? cuisineId, [FromQuery] Guid? countryId, CancellationToken cancellationToken)
        {
            // cuisineId = opened from that cuisine (its country's version); countryId = keep showing that version.
            return this.From(await sender.Send(new GetItemQuery(id, cuisineId, countryId), cancellationToken));
        }

        /// <summary>The current customer's own version of the dish (ingredients with flags, removed lines, steps).</summary>
        [HttpGet("{id}/mine")]
        public async Task<IActionResult> GetMine(Guid id, [FromQuery] Guid? countryId, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new GetMyItemQuery(id, countryId), cancellationToken));
        }

        public sealed record SaveMineBody(IReadOnlyList<MyLineInput>? Lines, IReadOnlyList<MyStepInput>? Steps, IReadOnlyList<Guid>? Parts = null, Guid? CountryId = null);

        /// <summary>Saves the customer's version; only the differences from the dish are stored.</summary>
        [HttpPut("{id}/mine")]
        public async Task<IActionResult> SaveMine(Guid id, [FromBody] SaveMineBody body, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new SaveMyItemCommand(id, body?.Lines, body?.Steps, body?.Parts, body?.CountryId), cancellationToken));
        }

        /// <summary>Back to the original dish.</summary>
        [HttpDelete("{id}/mine")]
        public async Task<IActionResult> ResetMine(Guid id, [FromQuery] Guid? countryId, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new ResetMyItemCommand(id, countryId), cancellationToken));
        }

        /// <summary>Dishes the customer can add as a part of this dish (تعديل الأكلة ليا).</summary>
        [HttpGet("{id}/part-picks")]
        public async Task<IActionResult> PartPicks(Guid id, string? text, int take, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new SearchPartPicksQuery(id, text, take <= 0 ? 20 : take), cancellationToken));
        }

        public sealed record ChoiceBody(int Kind, Guid RefId, bool Selected, Guid? CountryId = null);

        /// <summary>Picks or drops an optional ingredient (kind 0, refId = contentId) or optional part (kind 1, refId = part dish id).</summary>
        [HttpPut("{id}/choice")]
        public async Task<IActionResult> SetChoice(Guid id, [FromBody] ChoiceBody body, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new SetItemChoiceCommand(id, body.Kind, body.RefId, body.Selected, body.CountryId), cancellationToken));
        }

        [HttpPost("{id}/favorite")]
        public async Task<IActionResult> Favorite(Guid id, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new AddFavoriteCommand(id), cancellationToken));
        }

        [HttpDelete("{id}/favorite")]
        public async Task<IActionResult> Unfavorite(Guid id, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new RemoveFavoriteCommand(id), cancellationToken));
        }
    }
}
