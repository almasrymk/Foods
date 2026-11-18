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
            return ToActionResult(res);
        }

        [HttpPost("LoginCMS")]
        public async Task<IActionResult> LoginCMS([FromBody] LoginCMSCommand request, CancellationToken cancellationToken)
        {
            var res = await sender.Send(request, cancellationToken);
            return ToActionResult(res);
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterCommand request, CancellationToken cancellationToken)
        {
            var res = await sender.Send(request, cancellationToken);
            return ToActionResult(res);
        }

        [HttpGet("Countries")]
        public async Task<IActionResult> Countries(CancellationToken cancellationToken)
        {
            var res = await sender.Send(new Foods.Application.Customer.Profile.GetCountriesQuery(), cancellationToken);
            return ToActionResult(res);
        }

        private IActionResult ToActionResult<T>(Foods.Domain.Shared.Result<T> res)
        {
            return res.StatusCode switch
            {
                System.Net.HttpStatusCode.OK => Ok(res),
                System.Net.HttpStatusCode.Unauthorized => Unauthorized(res),
                System.Net.HttpStatusCode.NotFound => NotFound(res),
                _ => BadRequest(res)
            };
        }
    }
}