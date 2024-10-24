namespace Foods.API.Controllers
{
    using Foods.Core.Interface;
    using Microsoft.AspNetCore.Mvc;
    using Foods.Core.Entity.MasterData.Content;

    [ApiController]
    [Route("[controller]/[action]")]
    public class ContentGroupController(ILogger<ContentGroupController> logger, IContentGroupService contentGroupService) : ControllerBase
    {
        private readonly ILogger<ContentGroupController> _logger = logger;
        private readonly IContentGroupService _contentGroupService = contentGroupService;

        [HttpGet(Name = "GetList")]
        public async Task<IActionResult> GetList()
        {
            var list = await _contentGroupService.GetListByFilter();
            return Ok(list);
        }

        [HttpGet(Name = "Get")]
        public async Task<IActionResult> Get(Guid Id)
        {
            var ob = await _contentGroupService.GetByFilter(e=>e.Id == Id);
            return Ok(ob);
        }

        [HttpPost(Name = "Add")]
        public async Task<IActionResult> Add(ContentGroup model)
        {
            var ob = await _contentGroupService.Save(model);
            return Ok(ob);
        }


        [HttpPut(Name = "Edit")]
        public async Task<IActionResult> Edit(Guid Id , ContentGroup model)
        {
            var ob = await _contentGroupService.Save(model);
            return Ok(ob);
        }

        [HttpDelete(Name = "Delete")]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var ob = await _contentGroupService.Delete(e => e.Id == Id);
            return Ok(ob);
        }
    }
}