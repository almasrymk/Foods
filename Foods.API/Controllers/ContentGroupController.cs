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
        public async Task<IActionResult> Create(CreateCommand command ,CancellationToken cancellationToken)
        {
            var res = sender.Send(command, cancellationToken);
            return res.IsCompletedSuccessfully ? Ok(res) : BadRequest(res);
        }
    }
}