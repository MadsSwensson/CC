using CustomerCase.Functions.Subscribers.Customer;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerCase.Functions.DependencyInjection;

public static class SubscriberServiceExtensions
{
    public static IServiceCollection AddSubscriberServices(this IServiceCollection services)
    {
        services.AddScoped<IProcessCustomerRepository, ProcessCustomerRepository>();
        return services;
    }
}
