using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyLab.AsyncProcessor.Sdk;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.LogDsl;
using MyLab.Mq;

namespace MyLab.AsyncProcessor.Api.Services
{
    class DeadLetterConsumer : IMqConsumerLogic<QueueRequestMessage>
    {
        private readonly Logic _logic;
        private readonly DslLogger _log;

        public DeadLetterConsumer(Logic logic, ILogger<DeadLetterConsumer> logger)
        {
            _logic = logic;
            _log = logger.Dsl();
        }

        public async Task Consume(MqMessage<QueueRequestMessage> message)
        {
            await _logic.SetErrorAsync(message.Payload.Id, new ProcessingError
            {
               TechMessage = "The request was placed in the dead letter"
            });

            _log.Warning("The request was placed in the dead letter")
                .AndFactIs("request-id", message.Payload.Id)
                .Write();
        }
    }
}
