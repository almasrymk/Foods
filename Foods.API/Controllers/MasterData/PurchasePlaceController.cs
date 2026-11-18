namespace Foods.API.Controllers.MasterData
{
    using MediatR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Foods.Application.MasterData.PurchasePlace;

    [Authorize(Roles = Foods.Domain.Entity.Security.RoleNames.Admin)]
    [ApiController]
    [Route("[controller]/[action]")]
    public class PurchasePlaceController(ISender sender) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetList(CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new GetPurchasePlacesQuery(), cancellationToken));
        }

        [HttpGet]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new GetPurchasePlaceByIdQuery(id), cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePurchasePlaceCommand command, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(command, cancellationToken));
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdatePurchasePlaceCommand command, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(command, cancellationToken));
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new DeletePurchasePlaceCommand(id), cancellationToken));
        }
    }
}
