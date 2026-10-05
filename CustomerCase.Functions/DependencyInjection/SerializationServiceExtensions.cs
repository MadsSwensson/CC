using System.Text.Json.Serialization;
using CustomerCase.Functions.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerCase.Functions.DependencyInjection;

public static class SerializationServiceExtensions
{
    public static IServiceCollection AddSerializationServices(this IServiceCollection services)
    {
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNamingPolicy = new LowerCaseNamingPolicy();
            options.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            options.Converters.Add(new JsonStringEnumConverter());
        });
        
        services.AddScoped<IAppJsonSerializer, AppJsonSerializer>();
        
        return services;
    }
}