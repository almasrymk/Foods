namespace Foods.Infrastructure
{
    using Foods.Domain.Abstraction;
    using Foods.Infrastructure.Persistence;
    using Microsoft.Extensions.DependencyInjection;

    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            return services;
        }
    }
}