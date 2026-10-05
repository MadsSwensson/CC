using CustomerCase.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerCase.Functions.Publishers.Customer;

public class PublishCustomerFunction
{
    private const string FunctionName = "Publisher-Customer";

    private readonly IPublishCustomerRepository _repository;
    private readonly ILogger<PublishCustomerFunction> _logger;
    private readonly IOptions<JsonSerializerOptions> _jsonOptions;

    public PublishCustomerFunction(IPublishCustomerRepository repository, ILogger<PublishCustomerFunction> logger,
        IOptions<JsonSerializerOptions> jsonOptions)
    {
        _repository = repository;
        _logger = logger;
        _jsonOptions = jsonOptions;
    }

    [Function(FunctionName)]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers/{action}")]
        HttpRequestData req,
        string action)
    {
        var correlationId = Guid.NewGuid();

        using var scope = _logger.BeginScope(
            "CorrelationId={CorrelationId} FunctionName={FunctionName}",
            correlationId, FunctionName);
        
        _logger.LogInformation("Received customer '{Action}' request", action);

        var requestData = await JsonSerializer.DeserializeAsync<CustomerPublisherModel>(req.Body, _jsonOptions.Value);
        if (requestData == null || string.IsNullOrWhiteSpace(requestData.CustomerId))
        {
            _logger.LogError("Received customer '{Action}' request without a valid customerId", action);
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        if (!Enum.TryParse(action, out EventType eventType))
        {
            _logger.LogError("Received action '{Action}' for unknown event {EventType}", action, eventType);
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }


        await _repository.PublishCustomerAsync(requestData, eventType, correlationId);
        return req.CreateResponse(HttpStatusCode.OK);
    }
}