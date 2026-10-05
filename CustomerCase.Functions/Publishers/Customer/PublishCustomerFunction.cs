using CustomerCase.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CustomerCase.Functions.Publishers.Customer;

public class PublishCustomerFunction
{
    private const string FunctionName = "Publisher-Customer";

    private readonly IPublishCustomerRepository _repository;
    private readonly ILogger<PublishCustomerFunction> _logger;

    public PublishCustomerFunction(IPublishCustomerRepository repository, ILogger<PublishCustomerFunction> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [Function(FunctionName)]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers/{action}")] HttpRequestData req, 
        string action)
    {
        var correlationId = Guid.NewGuid();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["FunctionName"] = FunctionName
        });

        _logger.LogInformation("Received customer {Action} request", action);

        

        // _repository.PublishCustomerAsync()
        throw new NotImplementedException();
    }
}