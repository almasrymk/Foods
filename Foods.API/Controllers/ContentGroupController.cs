namespace Foods.API.Controllers
{
    using MediatR;
    using Microsoft.AspNetCore.Mvc;
    using Foods.Application.MasterData.Content.ContentCategory.Commands.Create;

    [ApiController]
    [Route("[controller]/[action]")]
    public class ContentGroupController(ISender sender) : ControllerBase
    {
        [HttpPost(Name = "Create")]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var ob = new CreateCommand("A+");
            var res = sender.Send(ob, cancellationToken);
            return res.IsCompletedSuccessfully ? Ok() : BadRequest();
        }
    }
}