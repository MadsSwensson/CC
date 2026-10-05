using CustomerCase.Functions.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerCase.Functions.DependencyInjection;

public static class SerializationServiceExtensions
{
    public static IServiceCollection AddSerializationServices(this IServiceCollection services)
    {
        services.AddScoped<IAppJsonSerializer, AppJsonSerializer>();
        
        return services;
    }
}