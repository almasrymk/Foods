namespace Foods.API.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// ,,n,,,m,,,n,
    /// </summary>
    /// <param name="logger"></param>
    [ApiController]
    [Route("[controller]")]
    public class ContentGroupController(ILogger<ContentGroupController> logger) : ControllerBase
    {
        [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>")]
        private readonly ILogger<ContentGroupController> _logger = logger;

        /// <summary>
        /// llkllllll
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost(Name = "Save")]        
        public IActionResult Save([FromBody] string model)
        {
            ArgumentNullException.ThrowIfNull(model);

            return Ok();
        }
    }
}