namespace Configuration;

public static class Constants
{
    public static class ServiceBus
    {
        public static class Topics
        {
            public const string Order = "website--order";
            public const string Customer = "website--customer";
        }

        public static class Subscriptions
        {
            public const string OrderProcessor = "order-processor";
            public const string CustomerProcessor = "customer-processor";
            public const string Local = "local";
        }
    }

    public static class Entities
    {
        public const string Order = "order";
        public const string Customer = "customer";
    }

    public static class Sources
    {
        public const string Website = "website";
    }
}
