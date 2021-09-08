using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.Log.Dsl;
using MyLab.RabbitClient.Consuming;

namespace MyLab.AsyncProcessor.Api.Services
{
    class DeadLetterConsumer : RabbitConsumer<QueueRequestMessage>
    {
        private readonly Logic _logic;
        private readonly IDslLogger _log;

        public DeadLetterConsumer(Logic logic, ILogger<DeadLetterConsumer> logger)
        {
            _logic = logic;
            _log = logger.Dsl();
        }

        protected override async Task ConsumeMessageAsync(ConsumedMessage<QueueRequestMessage> message)
        {
            await _logic.CompleteWithErrorAsync(message.Content.Id, new ProcessingError
            {
                TechMessage = "The request was placed in the dead letter"
            });

            _log.Warning("The request was placed in the dead letter")
                .AndFactIs("request-id", message.Content.Id)
                .Write();
        }
    }
}
