namespace Foods.API.Controllers
{
    using MediatR;
    using Microsoft.AspNetCore.Mvc;
    using Foods.Application.MasterData.Content.ContentCategory.Queries.Search;
    using Foods.Application.MasterData.Content.ContentCategory.Commands.Create;
    using Foods.Application.MasterData.Content.ContentCategory.Commands.Update;
    using Foods.Application.MasterData.Content.ContentCategory.Commands.Delete;
    using Foods.Application.MasterData.Content.ContentCategory.Queries.GetById;
    using Foods.Application.MasterData.Content.ContentCategory.Queries.GetList;

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

        [HttpGet(Name = "GetById")]
        public async Task<IActionResult> GetById(Guid Id, CancellationToken cancellationToken)
        {
            var res = sender.Send(new GetByIdQuery(Id), cancellationToken);
            return res.IsCompletedSuccessfully ? Ok(res) : BadRequest(res);
        }

        //[HttpGet(Name = "GetList")]
        //public async Task<IActionResult> GetList(string Name, CancellationToken cancellationToken)
        //{
        //    var res = null;// sender.Send(new GetListQuery(Name), cancellationToken);
        //    return res.IsCompletedSuccessfully ? Ok(res) : BadRequest(res);
        //}

        [HttpGet(Name = "Search")]
        public async Task<IActionResult> Search(string txtSearch, CancellationToken cancellationToken)
        {
            var res = sender.Send(new SearchQuery(txtSearch), cancellationToken);
            return res.IsCompletedSuccessfully ? Ok(res) : BadRequest(res);
        }
    }
}