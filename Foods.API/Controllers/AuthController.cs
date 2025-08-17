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
        public IActionResult Login([FromBody] LoginCommand request, CancellationToken cancellationToken)
        {
            var res = sender.Send(request, cancellationToken);
            return res.IsCompletedSuccessfully ? Ok(res) : BadRequest(res);
        }

        [HttpPost("Register")]
        public IActionResult Register([FromBody] LoginCommand request, CancellationToken cancellationToken)
        {
            var res = sender.Send(request, cancellationToken);
            return res.IsCompletedSuccessfully ? Ok(res) : BadRequest(res);
        }
    }
}