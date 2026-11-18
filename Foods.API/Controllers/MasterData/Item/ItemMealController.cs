namespace Foods.API.Controllers.MasterData.Item
{
    using MediatR;
    using System.Net;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using Foods.Application.MasterData.Item.ItemMeal.Queries.Search;
    using Foods.Application.MasterData.Item.ItemMeal.Queries.GetById;
    using Foods.Application.MasterData.Item.ItemMeal.Commands.Create;
    using Foods.Application.MasterData.Item.ItemMeal.Commands.Update;
    using Foods.Application.MasterData.Item.ItemMeal.Queries.GetList;
    using Foods.Application.MasterData.Item.ItemMeal.Commands.Delete;

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ItemMealController(ISender sender) : ControllerBase
    {
        [HttpGet("GetById")]
        public async Task<IActionResult> GetById(Guid Id, CancellationToken cancellationToken)
        {
            var res = await sender.Send(new GetByIdQuery(Id), cancellationToken);
            return res.StatusCode == HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }

        [HttpGet("GetList")]
        public async Task<IActionResult> GetList(Guid? ItemId, Guid? MealId,CancellationToken cancellationToken)
        {
            var res = await sender.Send(new GetListQuery(ItemId , MealId), cancellationToken);
            return res.StatusCode == HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }

        [HttpGet("Search")]
        public async Task<IActionResult> Search(Guid? ItemId, Guid? MealId, CancellationToken cancellationToken)
        {
            var res = await sender.Send(new SearchQuery(ItemId, MealId), cancellationToken);
            return res.StatusCode == HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CreateCommand Create, CancellationToken cancellationToken)
        {
            var res = await sender.Send(Create, cancellationToken);
            return res.StatusCode == HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }

        [HttpPut("Update")]
        public async Task<IActionResult> Update(UpdateCommand Update, CancellationToken cancellationToken)
        {
            var res = await sender.Send(Update, cancellationToken);
            return res.StatusCode == HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }

        [HttpDelete("Delete")]
        public async Task<IActionResult> Delete(Guid Id, CancellationToken cancellationToken)
        {
            var res = await sender.Send(new DeleteCommand(Id), cancellationToken);
            return res.StatusCode == HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }
    }
}