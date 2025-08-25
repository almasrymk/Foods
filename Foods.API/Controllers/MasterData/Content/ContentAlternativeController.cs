using MediatR;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Foods.Application.MasterData.Content.ContentAlternative.Queries.Search;
using Foods.Application.MasterData.Content.ContentAlternative.Queries.GetById;
using Foods.Application.MasterData.Content.ContentAlternative.Commands.Create;
using Foods.Application.MasterData.Content.ContentAlternative.Commands.Update;
using Foods.Application.MasterData.Content.ContentAlternative.Queries.GetList;
using Foods.Application.MasterData.Content.ContentAlternative.Commands.Delete;

namespace Foods.API.Controllers.MasterData.Content
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ContentAlternative(ISender sender) : ControllerBase
    {
        [HttpGet("GetById")]
        public async Task<IActionResult> GetById(Guid Id, CancellationToken cancellationToken)
        {
            var res = await sender.Send(new GetByIdQuery(Id), cancellationToken);
            return res.StatusCode == HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }

        [HttpGet("GetList")]
        public async Task<IActionResult> GetList(Guid? ContentId, Guid? ContentAlternativeId, CancellationToken cancellationToken)
        {
            var res = await sender.Send(new GetListQuery(ContentId, ContentAlternativeId), cancellationToken);
            return res.StatusCode == HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }

        [HttpGet("Search")]
        public async Task<IActionResult> Search(Guid? ContentId, Guid? ContentAlternativeId, CancellationToken cancellationToken)
        {
            var res = await sender.Send(new SearchQuery(ContentId, ContentAlternativeId), cancellationToken);
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