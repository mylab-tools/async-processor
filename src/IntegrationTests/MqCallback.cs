using System;
using MyLab.RabbitClient.Model;

namespace IntegrationTests
{
    class MqCallback : IDisposable
    {
        public RabbitQueue IncomingMq { get; }
        public RabbitExchange Exchange { get; }

        public MqCallback(RabbitQueue incomingMq, RabbitExchange exchange)
        {
            IncomingMq = incomingMq;
            Exchange = exchange;
        }

        public void Dispose()
        {
            Exchange.Remove();
            IncomingMq.Remove();
        }
    }
}