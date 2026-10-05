using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Infrastructure.Models;

namespace Infrastructure.Factories;

public class MessageFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ServiceBusMessage CreateMessage<T>(MessageBody<T> messageBody)
    {
        var json = JsonSerializer.Serialize(messageBody, SerializerOptions);

        var message = new ServiceBusMessage(json)
        {
            ContentType = "application/json",
            CorrelationId = messageBody.CorrelationId.ToString(),
            Subject = messageBody.EntityName
        };

        return message;
    }
}
