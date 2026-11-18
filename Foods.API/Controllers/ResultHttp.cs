namespace Foods.API.Controllers
{
    using System.Net;
    using Microsoft.AspNetCore.Mvc;
    using Foods.Domain.Shared;

    public static class ResultHttp
    {
        public static IActionResult From<T>(this ControllerBase controller, Result<T> res)
        {
            return res.StatusCode switch
            {
                HttpStatusCode.OK => controller.Ok(res),
                HttpStatusCode.Unauthorized => controller.Unauthorized(res),
                HttpStatusCode.NotFound => controller.NotFound(res),
                _ => controller.BadRequest(res)
            };
        }
    }
}
