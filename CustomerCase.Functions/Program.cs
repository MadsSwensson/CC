using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using CustomerCase.Functions;
using CustomerCase.Functions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.SetBasePath(context.HostingEnvironment.ContentRootPath);
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNamingPolicy = new LowerCaseNamingPolicy();
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });   
        services.AddInfrastructureServices(context.Configuration);
        services.AddPublisherServices();
        services.AddSubscriberServices();
    })
    .Build();

host.Run();
