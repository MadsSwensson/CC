using Azure.Messaging.ServiceBus;
using Infrastructure.Models;
using Infrastructure.Serialization;

namespace Infrastructure.Factories;

public static class MessageFactory
{
    public static ServiceBusMessage CreateMessage<T>(MessageBody<T> messageBody)
    {
        var json = SharedJsonSerializer.Serialize(messageBody);

        var message = new ServiceBusMessage(json)
        {
            ContentType = "application/json",
            CorrelationId = messageBody.CorrelationId.ToString(),
            Subject = messageBody.EntityName
        };

        return message;
    }
}
