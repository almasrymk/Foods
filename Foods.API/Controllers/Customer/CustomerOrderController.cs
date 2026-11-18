namespace Foods.API.Controllers.Customer
{
    using MediatR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Foods.Domain.Entity.Security;
    using Foods.Application.Customer.Ordering;

    [Authorize(Roles = $"{RoleNames.Customer},{RoleNames.Admin}")]
    [ApiController]
    [Route("customer/orders")]
    public class CustomerOrderController(ISender sender) : ControllerBase
    {
        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate([FromBody] List<ItemSelection> items, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new CalculateOrderQuery(items), cancellationToken));
        }

        [HttpGet]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new GetOrdersQuery(), cancellationToken));
        }

        /// <summary>Old orders (confirmed only), newest first, 20 per page, with search and status filter.</summary>
        [HttpGet("history")]
        public async Task<IActionResult> History([FromQuery] OrderHistoryQuery query, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(query, cancellationToken));
        }

        /// <summary>Marks the purchase order as printed.</summary>
        [HttpPost("{id}/printed")]
        public async Task<IActionResult> Printed(Guid id, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new MarkOrderPrintedCommand(id), cancellationToken));
        }

        /// <summary>Marks the order as shared.</summary>
        [HttpPost("{id}/shared")]
        public async Task<IActionResult> Shared(Guid id, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new MarkOrderSharedCommand(id), cancellationToken));
        }

        /// <summary>Marks the order as re-ordered (its meals were added to the current selection).</summary>
        [HttpPost("{id}/reordered")]
        public async Task<IActionResult> Reordered(Guid id, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new MarkOrderReorderedCommand(id), cancellationToken));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new GetOrderQuery(id), cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaveOrderCommand command, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(command, cancellationToken));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveOrderCommand command, CancellationToken cancellationToken)
        {
            var update = command with { Id = id };
            return this.From(await sender.Send(update, cancellationToken));
        }

        public sealed record RenameOrderBody(string Name);

        [HttpPut("{id}/name")]
        public async Task<IActionResult> Rename(Guid id, [FromBody] RenameOrderBody body, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new RenameOrderCommand(id, body?.Name ?? string.Empty), cancellationToken));
        }

        [HttpPost("{id}/reorder")]
        public async Task<IActionResult> Reorder(Guid id, CancellationToken cancellationToken)
        {
            return this.From(await sender.Send(new ReorderCommand(id), cancellationToken));
        }
    }
}
