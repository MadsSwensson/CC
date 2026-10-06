using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.ServiceBus;
using Infrastructure.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CustomerCase.Functions.Subscribers.Customer;

/// <summary>
/// Creates or updates customers from the website--customer topic and settles every message <b>explicitly</b>
/// (host.json: autoCompleteMessages = false).
/// <list type="bullet">
/// <item>Success: complete.</item>
/// <item>Invalid message or rejected by the repository: dead-letter immediately, retrying cannot help.</item>
/// <item>Exception, 5xx or 404 (update arrived before its create): abandon and rethrow for an immediate retry.
/// The subscription's MaxDeliveryCount (3) makes the broker dead-letter after the last attempt.</item>
/// <item>Cancellation: not settled; the lock expires and the message is redelivered.</item>
/// </list>
/// </summary>
public class ProcessCustomerFunction
{
    private const string FunctionName = "Subscriber-Customer-Website-Customer";
    private const string Topic = Constants.ServiceBus.Topics.Customer;
    private const string Subscription = Constants.ServiceBus.Subscriptions.CustomerProcessor;

    private readonly IProcessCustomerRepository _repository;
    
    private readonly ILogger<ProcessCustomerFunction> _logger;

    public ProcessCustomerFunction(IProcessCustomerRepository repository, ILogger<ProcessCustomerFunction> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [Function(FunctionName)]
    public async Task Run(
        [ServiceBusTrigger(Topic, Subscription, Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        var correlationId = Guid.TryParse(message.CorrelationId, out var id) ? id : Guid.NewGuid();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["FunctionName"] = FunctionName,
            ["Topic"] = Topic,
            ["Subscription"] = Subscription
        });

        if (!TryRead(message, out var body, out var invalidReason))
        {
            await messageActions.DeadLetterMessageAsync(message, deadLetterReason: invalidReason);
            return;
        }

        var customer = body.Payload.EntityData;
        HttpStatusCode status;
        try
        {
            status = await CreateOrUpdate(body.EventType, customer);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await RetryAsync(message, messageActions, ex);
            throw;
        }

        switch (GetOutcome(status))
        {
            case Outcome.Complete:
                await messageActions.CompleteMessageAsync(message);
                _logger.LogInformation("Processed {EventType} for customer {CustomerId}",
                    body.EventType, customer.CustomerId);
                break;

            case Outcome.DeadLetter:
                _logger.LogWarning("Customer {EventType} rejected with {StatusCode} for {CustomerId}",
                    body.EventType, status, customer.CustomerId);
                await messageActions.DeadLetterMessageAsync(
                    message, deadLetterReason: $"CustomerRejected_{(int)status}");
                break;

            default:
                var failure = new InvalidOperationException(
                    $"Customer {body.EventType} failed with status {(int)status}.");
                await RetryAsync(message, messageActions, failure);
                throw failure;
        }
    }

    private bool TryRead(
        ServiceBusReceivedMessage message,
        [NotNullWhen(true)] out MessageBody<CustomerPublisherModel>? body,
        [NotNullWhen(false)] out string? invalidReason)
    {
        body = null;
        try
        {
            body = SharedJsonSerializer.Deserialize<MessageBody<CustomerPublisherModel>>(message.Body);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Customer message contains invalid JSON");
            invalidReason = "InvalidJson";
            return false;
        }

        if (string.IsNullOrWhiteSpace(body?.Payload?.EntityData?.CustomerId))
        {
            _logger.LogError("Customer message is missing customer data or customerId");
            invalidReason = "InvalidCustomerData";
            return false;
        }

        if (body.EventType is not (EventType.Create or EventType.Update))
        {
            _logger.LogWarning("Unsupported customer event type {EventType}", body.EventType);
            invalidReason = "UnsupportedEventType";
            return false;
        }

        invalidReason = null;
        return true;
    }

    private async Task<HttpStatusCode> CreateOrUpdate(EventType eventType, CustomerPublisherModel customer)
    {
        var result = eventType == EventType.Create
            ? await _repository.CreateCustomerAsync(customer)
            : await _repository.UpdateCustomerAsync(customer);

        return result.StatusCode;
    }

    private static Outcome GetOutcome(HttpStatusCode status) =>
        status switch
        {
            HttpStatusCode.OK or HttpStatusCode.Created => Outcome.Complete,
            // An update can arrive before its create; retrying gives the create time to land.
            HttpStatusCode.NotFound => Outcome.Retry,
            >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError => Outcome.DeadLetter,
            _ => Outcome.Retry
        };

    private async Task RetryAsync(
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions,
        Exception exception)
    {
        _logger.LogWarning(
            exception,
            "Customer message failed on attempt {DeliveryCount}; abandoning for retry",
            message.DeliveryCount);
        try
        {
            await messageActions.AbandonMessageAsync(message);
        }
        catch (Exception ex)
        {
            // The lock expires and the broker redelivers anyway, so keep the original failure.
            _logger.LogWarning(ex, "Failed to abandon customer message");
        }
    }

    private enum Outcome
    {
        Complete,
        Retry,
        DeadLetter
    }
}