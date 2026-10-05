namespace Configuration;

public record TopicRegistration(string TopicName, params string[] Subscriptions);

public static class TopicsAndSubscriptionsRegistry
{
    public static readonly List<TopicRegistration> All =
    [
        new TopicRegistration(
            Constants.ServiceBus.Topics.Order,
            Constants.ServiceBus.Subscriptions.OrderProcessor),

        new TopicRegistration(
            Constants.ServiceBus.Topics.Customer,
            Constants.ServiceBus.Subscriptions.CustomerProcessor)
    ];
}
