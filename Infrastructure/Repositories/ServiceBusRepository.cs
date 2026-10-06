using Azure.Messaging.ServiceBus;
using Infrastructure.Factories;
using Infrastructure.Models;
using Infrastructure.Providers;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

public interface IServiceBusRepository
{
    Task WriteToTopic<T>(MessageBody<T> messageBody);
}

public class ServiceBusRepository : IServiceBusRepository
{
    private readonly ServiceBusSenderProvider _senderProvider;
    private readonly ILogger<ServiceBusRepository> _logger;

    public ServiceBusRepository(
        ServiceBusSenderProvider senderProvider,
        ILogger<ServiceBusRepository> logger)
    {
        _senderProvider = senderProvider;
        _logger = logger;
    }

    public async Task WriteToTopic<T>(MessageBody<T> messageBody)
    {
        var sender = _senderProvider.GetSender(messageBody.Topic);
        var message = MessageFactory.CreateMessage(messageBody);
        await sender.SendMessageAsync(message);

        _logger.LogInformation(
            "Published message to topic {Topic} with CorrelationId {CorrelationId}",
            messageBody.Topic, messageBody.CorrelationId);
    }
}
