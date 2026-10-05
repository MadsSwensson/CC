using Azure.Messaging.ServiceBus;
using CustomerCase.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CustomerCase.Functions.Subscribers.Customer;

public class ProcessCustomerFunction
{
    private const string FunctionName = "Subscriber-Customer-Website-Customer";
    private const string Topic = Constants.ServiceBus.Topics.Customer;
    private const string Subscription = Constants.ServiceBus.Subscriptions.CustomerProcessor;

    private readonly IProcessCustomerRepository _repository;
    private readonly ILogger<ProcessCustomerFunction> _logger;

    public ProcessCustomerFunction(
        IProcessCustomerRepository repository,
        ILogger<ProcessCustomerFunction> logger)
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

        FunctionResponseModel response = new() { StatusCode = HttpStatusCode.InternalServerError };

        try
        {
            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process customer message");
        }
        finally
        {

        }
    }
}
