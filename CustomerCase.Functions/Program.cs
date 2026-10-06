using CustomerCase.Functions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureLogging((context, logging) =>
    {
        logging.AddConfiguration(context.Configuration.GetSection("Logging"));
        logging.AddSimpleConsole();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSerializationServices();
        services.AddInfrastructureServices(context.Configuration);
        services.AddPublisherServices();
        services.AddSubscriberServices();
    })
    .Build();

host.Run();
