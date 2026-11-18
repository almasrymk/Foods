namespace Foods.API.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    [ApiController]
    [Route("files")]
    public class FilesController(IWebHostEnvironment environment) : ControllerBase
    {
        private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        [HttpPost("image")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { errors = new[] { "اختر صورة" } });

            var extension = Path.GetExtension(file.FileName);
            if (!Allowed.Contains(extension))
                return BadRequest(new { errors = new[] { "الصورة يجب أن تكون jpg أو png أو webp" } });

            var folder = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads");
            Directory.CreateDirectory(folder);
            var name = Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();
            var fullPath = Path.Combine(folder, name);
            await using var stream = System.IO.File.Create(fullPath);
            await file.CopyToAsync(stream, cancellationToken);
            return Ok(new { path = "/uploads/" + name });
        }
    }
}
