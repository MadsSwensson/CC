using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;

namespace Infrastructure.Providers;

public class ServiceBusSenderProvider
{
    private readonly ServiceBusClient _client;
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();

    public ServiceBusSenderProvider(ServiceBusClient client)
    {
        _client = client;
    }

    public ServiceBusSender GetSender(string topicName)
    {
        return _senders.GetOrAdd(topicName, name => _client.CreateSender(name));
    }
}
