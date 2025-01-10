namespace Foods.API.Controllers
{
    using MediatR;
    using Microsoft.AspNetCore.Mvc;
    using Foods.Application.MasterData.Content.ContentCategory.Commands.Create;
    using Foods.Application.MasterData.Content.ContentCategory.Commands.Update;
    using Foods.Application.MasterData.Content.ContentCategory.Commands.Delete;

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

        [HttpPut(Name = "Update")]
        public async Task<IActionResult> Update(UpdateCommand command, CancellationToken cancellationToken)
        {
            var res = sender.Send(command, cancellationToken);
            return res.IsCompletedSuccessfully ? Ok(res) : BadRequest(res);
        }

        [HttpDelete(Name = "Delete")]
        public async Task<IActionResult> Delete( Guid Id, CancellationToken cancellationToken)
        {
            var res = sender.Send(new DeleteCommand(Id), cancellationToken);
            return res.IsCompletedSuccessfully ? Ok(res) : BadRequest(res);
        }
    }
}