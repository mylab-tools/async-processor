using MyLab.Mq;
using MyLab.Mq.Communication;
using MyLab.Mq.MqObjects;

namespace IntegrationTests
{
    static class TestTools
    {
        public static readonly MqOptions MqOptions = new MqOptions
        {
            Host = "localhost",
            Port = 10202,
            User = "guest",
            Password = "guest"
        };

        public static MqExchangeFactory ExchangeFactory(MqExchangeType type = MqExchangeType.Direct,
            bool autoDelete = true)
        {
            return new MqExchangeFactory(
                type,
                new MqChannelProvider(new DefaultMqConnectionProvider(MqOptions)
                ))
            {
                AutoDelete = autoDelete
            };
        }

        public static MqQueueFactory QueueFactory(bool autoDelete = true)
        {
            return new MqQueueFactory(
                new MqChannelProvider(new DefaultMqConnectionProvider(MqOptions)
                ))
            {
                AutoDelete = autoDelete
            };
        }
    }
}
