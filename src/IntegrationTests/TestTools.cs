using MyLab.RabbitClient;
using MyLab.RabbitClient.Connection;
using MyLab.RabbitClient.Model;

namespace IntegrationTests
{
    static class TestTools
    {
        public static readonly RabbitOptions MqOptions = new RabbitOptions
        {
            Host = "localhost",
            Port = 10202,
            User = "guest",
            Password = "guest"
        };

        public static RabbitExchangeFactory ExchangeFactory(RabbitExchangeType type = RabbitExchangeType.Direct,
            bool autoDelete = true)
        {
            return new RabbitExchangeFactory(
                type,
                new RabbitChannelProvider(new LazyRabbitConnectionProvider(MqOptions)
                ))
            {
                AutoDelete = autoDelete
            };
        }

        public static RabbitQueueFactory QueueFactory(bool autoDelete = true)
        {
            return new RabbitQueueFactory(
                new RabbitChannelProvider(new LazyRabbitConnectionProvider(MqOptions)
                ))
            {
                AutoDelete = autoDelete
            };
        }
    }
}
