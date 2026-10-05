using Azure.Messaging.ServiceBus;
using CustomerCase.Functions.Data;
using Infrastructure.Factories;
using Infrastructure.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerCase.Functions.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ServiceBusConnection"];
        services.AddSingleton(new ServiceBusClient(connectionString));
        services.AddSingleton<ServiceBusSenderProvider>();
        services.AddSingleton<MessageFactory>();
        services.AddScoped<IServiceBusRepository, ServiceBusRepository>();
        services.AddSingleton<ICustomerDatabase, JsonFileCustomerDatabase>();
        
        return services;
    }
}
