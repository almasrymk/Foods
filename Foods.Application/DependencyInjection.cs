namespace Foods.Application
{
    using FluentValidation;
    using Microsoft.Extensions.DependencyInjection;
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(configuration => 
                configuration.RegisterServicesFromAssembly(assembly));

            services.AddValidatorsFromAssembly(assembly);
            services.AddScoped<Foods.Application.Customer.Ordering.IOrderCalculator, Foods.Application.Customer.Ordering.OrderCalculator>();
            services.AddScoped<Foods.Application.Customer.Notifications.NotificationGenerator>();

            return services;
        }
    }
}