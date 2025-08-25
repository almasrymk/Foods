using MediatR;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Foods.Application.MasterData.Content.Content.Queries.Search;
using Foods.Application.MasterData.Content.Content.Queries.GetById;
using Foods.Application.MasterData.Content.Content.Commands.Create;
using Foods.Application.MasterData.Content.Content.Commands.Update;
using Foods.Application.MasterData.Content.Content.Queries.GetList;
using Foods.Application.MasterData.Content.Content.Commands.Delete;

namespace Foods.API.Controllers.MasterData.Content
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ContentController(ISender sender) : ControllerBase
    {
        [HttpGet("GetById")]
        public async Task<IActionResult> GetById(Guid Id, CancellationToken cancellationToken)
        {
            var res = await sender.Send(new GetByIdQuery(Id), cancellationToken);
            return res.StatusCode == HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }

        [HttpGet("GetList")]
        public async Task<IActionResult> GetList(Guid? ContentCategoryId, Guid? ContentGroupId, CancellationToken cancellationToken)
        {
            var res = await sender.Send(new GetListQuery(ContentCategoryId, ContentGroupId), cancellationToken);
            return res.StatusCode == HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }

        [HttpGet("Search")]
        public async Task<IActionResult> Search(string TextSearch, Guid? ContentCategoryId, Guid? ContentGroupId , CancellationToken cancellationToken)
        {
            var res = await sender.Send(new SearchQuery(TextSearch , ContentCategoryId, ContentGroupId), cancellationToken);
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