using Azure.Messaging.ServiceBus;
using CustomerCase.Functions.Serialization;
using Microsoft.Extensions.Logging;

namespace CustomerCase.Functions.Publishers.Customer;

public interface IPublishCustomerRepository
{
    Task<FunctionResponseModel> PublishCustomerAsync(CustomerPublisherModel message, EventType eventType, Guid correlationId);
}

public class PublishCustomerRepository : IPublishCustomerRepository
{
    private readonly IServiceBusRepository _serviceBusRepository;
    private readonly ILogger<PublishCustomerRepository> _logger;
    private readonly IAppJsonSerializer _serializer;

    public PublishCustomerRepository(
        IServiceBusRepository serviceBusRepository,
        ILogger<PublishCustomerRepository> logger,
        IAppJsonSerializer serializer)
    {
        _serviceBusRepository = serviceBusRepository;
        _logger = logger;
        _serializer = serializer;
    }

    public async Task<FunctionResponseModel> PublishCustomerAsync(CustomerPublisherModel message, EventType eventType, Guid correlationId)
    {
        var messageBody = ComposeMessageBody(message, eventType, correlationId);

        try
        {
            await _serviceBusRepository.WriteToTopic(messageBody);
            _logger.LogInformation($"Published Customer: {_serializer.Serialize(messageBody)}");
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

    private static List<string> GetChangedFields(CustomerPublisherModel message)
    {
        return typeof(CustomerPublisherModel).GetProperties()
            .Where(p => p.Name != nameof(CustomerPublisherModel.CustomerId)
                        && p.GetValue(message) is not null)
            .Select(p => p.Name)
            .ToList();
    }
}