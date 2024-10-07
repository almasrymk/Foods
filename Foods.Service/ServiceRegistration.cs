namespace Foods.Service
{
    using Foods.Core.Interface;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceRegistration
    {
        public static void AddServiceRegistration(this WebApplicationBuilder builder)
        {
            builder.Services.AddTransient<IContentGroupService, ContentGroupService>();
        }
    }
}