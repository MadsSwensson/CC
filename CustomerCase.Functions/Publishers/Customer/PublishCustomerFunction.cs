using CustomerCase.Functions.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CustomerCase.Functions.Publishers.Customer;

public class PublishCustomerFunction
{
    private const string FunctionName = "Publisher-Customer";

    private readonly IPublishCustomerRepository _repository;
    private readonly ILogger<PublishCustomerFunction> _logger;
    private readonly IAppJsonSerializer _serializer;

    public PublishCustomerFunction(IPublishCustomerRepository repository, ILogger<PublishCustomerFunction> logger,
        IAppJsonSerializer serializer)
    {
        _repository = repository;
        _logger = logger;
        _serializer = serializer;
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

        CustomerPublisherModel? requestData;
        try
        {
            requestData = await _serializer.DeserializeAsync<CustomerPublisherModel>(req.Body);
        }
        catch (JsonException e)
        {
            _logger.LogWarning(e,"Received customer '{Action}' request with invalid customer data", action);
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }
        
        if (requestData == null || string.IsNullOrWhiteSpace(requestData.CustomerId))
        {
            _logger.LogWarning("Received customer '{Action}' request without a valid customerId", action);
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        if (!Enum.TryParse(action, ignoreCase: true, out EventType eventType))
        {
            _logger.LogWarning("Received action '{Action}' for unknown event {EventType}", action, eventType);
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        if (eventType == EventType.Delete)
        {
            _logger.LogError("Received action '{Action}' for deleting customer event. Not yet supported.", action);
            return req.CreateResponse(HttpStatusCode.NotImplemented);
        }
        
        await _repository.PublishCustomerAsync(requestData, eventType, correlationId);
        return req.CreateResponse(HttpStatusCode.OK);
    }
}