namespace Foods.Infrastructure
{
    using Foods.Domain.Abstraction;
    using Foods.Application.Abstraction.Security;
    using Foods.Infrastructure.Persistence.UnitOfWork;
    using Foods.Infrastructure.Security;
    using Microsoft.Extensions.DependencyInjection;

    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<ICurrentUser, CurrentUser>();
            return services;
        }
    }
}