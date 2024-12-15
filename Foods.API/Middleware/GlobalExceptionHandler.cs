namespace Foods.API.Middleware
{
    using System.Text.Json;
    using Microsoft.AspNetCore.Diagnostics;    

    public static class GlobalExceptionHandler
    {
        public static void HandlerException(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(e => e.Run(async context => { 
                var errorFeature =  context.Features.Get<IExceptionHandlerFeature>();
                var exception = errorFeature.Error;

                if (exception is not FluentValidation.ValidationException validationException)
                    throw exception;

                var errors = validationException.Errors.Select(e=> new { Property = e.PropertyName , Message = e.ErrorMessage });

                var errorContent =  JsonSerializer.Serialize(errors);
            }));
        }
    }
}