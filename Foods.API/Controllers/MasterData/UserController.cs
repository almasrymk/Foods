namespace Foods.API.Controllers.MasterData
{
    using System.Net;
    using Foods.Application.Security.Users.Commands.Create;
    using Foods.Application.Security.Users.Commands.Delete;
    using Foods.Application.Security.Users.Commands.Update;
    using Foods.Application.Security.Users.Queries.GetById;
    using Foods.Application.Security.Users.Queries.GetList;
    using MediatR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UserController(ISender sender) : ControllerBase
    {
        [HttpGet("GetList")]
        public async Task<IActionResult> GetList(CancellationToken cancellationToken)
        {
            var res = await sender.Send(new GetListQuery(), cancellationToken);
            return res.StatusCode == HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }

        [HttpGet("GetById")]
        public async Task<IActionResult> GetById(Guid Id, CancellationToken cancellationToken)
        {
            var res = await sender.Send(new GetByIdQuery(Id), cancellationToken);
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
