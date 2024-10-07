namespace Foods.Infrastructure
{
    using Foods.Core.Interface;
    using Foods.Infrastructure.Persistence;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    public static class InfrastructureRegistration
    {
        public static void AddInfrastructureRegistration(this WebApplicationBuilder builder)
        {
            builder.Services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
        }
    }
}