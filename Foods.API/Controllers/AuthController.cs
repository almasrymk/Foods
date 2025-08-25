namespace Foods.API.Controllers
{
    using MediatR;
    using System.Threading;
    using Microsoft.AspNetCore.Mvc;
    using Foods.Application.Security.Queries.Create;

    [ApiController]
    [Route("[controller]")]
    public class AuthController(ISender sender) : ControllerBase
    {
        [HttpPost("Login")]
        public async Task< IActionResult> Login([FromBody] LoginCommand request, CancellationToken cancellationToken)
        {
            var res = await sender.Send(request, cancellationToken);
            return res.StatusCode == System.Net.HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }

        [HttpPost("LoginCMS")]
        public async Task<IActionResult> LoginCMS([FromBody] LoginCMSCommand request, CancellationToken cancellationToken)
        {
            var res = await sender.Send(request, cancellationToken);
            return res.StatusCode == System.Net.HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterCommand request, CancellationToken cancellationToken)
        {
            var res = await sender.Send(request, cancellationToken);
            return res.StatusCode == System.Net.HttpStatusCode.OK ? Ok(res) : BadRequest(res);
        }
    }
}