using CustomerCase.Functions.Publishers.Customer;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerCase.Functions.DependencyInjection;

public static class PublisherServiceExtensions
{
    public static IServiceCollection AddPublisherServices(this IServiceCollection services)
    {   
        services.AddScoped<IPublishCustomerRepository, PublishCustomerRepository>();
        return services;
    }
}
