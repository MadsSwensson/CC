using CustomerCase.Functions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddInfrastructureServices(context.Configuration);
        services.AddPublisherServices();
        services.AddSubscriberServices();
    })
    .Build();

host.Run();
