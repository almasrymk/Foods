namespace Foods.API.Middleware
{
    using System.Net;
    using Microsoft.AspNetCore.Diagnostics;

    public static class GlobalExceptionHandler
    {
        public static void HandlerException(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(handler => handler.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                context.Response.ContentType = "application/json";

                if (exception is FluentValidation.ValidationException validationException)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        statusCode = (int)HttpStatusCode.BadRequest,
                        response = (object?)null,
                        errors = validationException.Errors.Select(e => e.ErrorMessage).ToArray()
                    });
                    return;
                }

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = (int)HttpStatusCode.InternalServerError,
                    response = (object?)null,
                    errors = new[] { "حدث خطأ" }
                });
            }));
        }
    }
}
