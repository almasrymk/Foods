namespace Foods.API.Controllers.MasterData
{
    using System.Net;
    using MediatR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Foods.Application.MasterData.Country.Commands.Create;
    using Foods.Application.MasterData.Country.Commands.Delete;
    using Foods.Application.MasterData.Country.Commands.Update;
    using Foods.Application.MasterData.Country.Queries.GetById;
    using Foods.Application.MasterData.Country.Queries.GetList;
    using Foods.Application.MasterData.Country.Queries.Search;

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class CountryController(ISender sender) : ControllerBase
    {
        [HttpGet("GetById")]
        public async Task<IActionResult> GetById(Guid Id, CancellationToken cancellationToken)
        {
            var res = await sender.Send(new GetByIdQuery(Id), cancellationToken);
            return res.StatusCode == HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }

        [HttpGet("GetList")]
        public async Task<IActionResult> GetList(CancellationToken cancellationToken)
        {
            var res = await sender.Send(new GetListQuery(), cancellationToken);
            return res.StatusCode == HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }

        [HttpGet("Search")]
        public async Task<IActionResult> Search(string TextSearch, CancellationToken cancellationToken)
        {
            var res = await sender.Send(new SearchQuery(TextSearch), cancellationToken);
            return res.StatusCode == HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] CreateCommand command, CancellationToken cancellationToken)
        {
            var res = await sender.Send(command, cancellationToken);
            return res.StatusCode == HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }

        [HttpPut("Update")]
        public async Task<IActionResult> Update(UpdateCommand command, CancellationToken cancellationToken)
        {
            var res = await sender.Send(command, cancellationToken);
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
