using Infrastructure.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CustomerCase.Functions.Publishers.Customer;

/// <summary>
/// Accepts customer create/update requests on <c>POST /api/customers/{action}</c> and publishes them
/// to the website--customer topic. Each request gets a new correlationId, which travels with the message
/// so publisher and subscriber logs can be tied together.
/// <list type="bullet">
/// <item><c>action</c> is <c>create</c> or <c>update</c> (case-insensitive); anything else: 400.</item>
/// <item>Invalid JSON, empty body or missing/blank <c>customerId</c>: 400, nothing is published.</item>
/// <item>Published: the repository's status is returned (202 Accepted).</item>
/// <item>Service Bus unavailable: the repository's status is returned (503), so the caller can retry.</item>
/// </list>
/// </summary>
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers/{action}")]
        HttpRequestData req,
        string action)
    {
        var correlationId = Guid.NewGuid();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["FunctionName"] = FunctionName
        });
        
        _logger.LogInformation("Received customer '{Action}' request", action);

        CustomerPublisherModel? requestData;
        try
        {
            requestData = await SharedJsonSerializer.DeserializeAsync<CustomerPublisherModel>(req.Body);
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

        if (!Enum.TryParse(action, ignoreCase: true, out EventType eventType) ||
            eventType is not (EventType.Create or EventType.Update))
        {
            _logger.LogWarning("Received unsupported customer action '{Action}'", action);
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var result = await _repository.PublishCustomerAsync(requestData, eventType, correlationId);
        return req.CreateResponse(result.StatusCode);
    }
}