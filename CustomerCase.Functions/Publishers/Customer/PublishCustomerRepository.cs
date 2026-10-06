using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace CustomerCase.Functions.Publishers.Customer;

public interface IPublishCustomerRepository
{
    Task<FunctionResponseModel> PublishCustomerAsync(CustomerPublisherModel message, EventType eventType, Guid correlationId);
}

/// <summary>
/// Wraps a customer in a <see cref="MessageBody{T}"/> envelope and writes it to the website--customer topic.
/// <list type="bullet">
/// <item>The envelope carries the event type, timestamp, entity name/source and the caller's correlationId.</item>
/// <item><c>ChangedFields</c> lists the non-null fields (excluding <c>CustomerId</c>), so the subscriber knows
/// which fields an update sets.</item>
/// <item>Published: 202 Accepted.</item>
/// <item><see cref="ServiceBusException"/>: 503 Service Unavailable with the failure reason; other exceptions propagate.</item>
/// </list>
/// </summary>
public class PublishCustomerRepository : IPublishCustomerRepository
{
    private readonly IServiceBusRepository _serviceBusRepository;
    private readonly ILogger<PublishCustomerRepository> _logger;

    public PublishCustomerRepository(IServiceBusRepository serviceBusRepository, ILogger<PublishCustomerRepository> logger)
    {
        _serviceBusRepository = serviceBusRepository;
        _logger = logger;
    }

    public async Task<FunctionResponseModel> PublishCustomerAsync(CustomerPublisherModel message, EventType eventType, Guid correlationId)
    {
        var messageBody = ComposeMessageBody(message, eventType, correlationId);

        try
        {
            await _serviceBusRepository.WriteToTopic(messageBody);
            _logger.LogInformation("Published Customer {CustomerId}", message.CustomerId);
            
            return new FunctionResponseModel
            {
                StatusCode = HttpStatusCode.Accepted,
                Subject = message.CustomerId
            };
        }
        catch (ServiceBusException exception)
        {
            _logger.LogError(exception, "Failed to publish customer {CustomerId}", message.CustomerId);
            return new FunctionResponseModel
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Subject = message.CustomerId,
                AdditionalDescription = exception.Reason.ToString()
            };
        }
    }

    private static MessageBody<CustomerPublisherModel> ComposeMessageBody(CustomerPublisherModel message, EventType eventType,
        Guid correlationId)
    {
        var messageBody = new MessageBody<CustomerPublisherModel>
        {
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            EntityName = Constants.Entities.Customer,
            EntitySource = Constants.Sources.Website,
            CorrelationId = correlationId,
            Payload = new Payload<CustomerPublisherModel>
            {
                EntityData = message,
                ChangedFields = GetChangedFields(message)
            }
        };
        return messageBody;
    }

    /// <summary>Names of the non-null properties, in declaration order, excluding <c>CustomerId</c>.</summary>
    public static List<string> GetChangedFields(CustomerPublisherModel message)
    {
        return typeof(CustomerPublisherModel).GetProperties()
            .Where(p => p.Name != nameof(CustomerPublisherModel.CustomerId)
                        && p.GetValue(message) is not null)
            .Select(p => p.Name)
            .ToList();
    }
}