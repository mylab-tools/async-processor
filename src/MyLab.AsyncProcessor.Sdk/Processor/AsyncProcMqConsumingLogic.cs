using System;
using System.Threading.Tasks;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.Mq;
using MyLab.Mq.PubSub;
using Newtonsoft.Json;

namespace MyLab.AsyncProcessor.Sdk.Processor
{
    class AsyncProcMqConsumingLogic<T> : IMqConsumerLogic<QueueRequestMessage>
    {
        private readonly IAsyncProcessorRequestsApi _api;
        private readonly IAsyncProcessingLogic<T> _logic;

        public AsyncProcMqConsumingLogic(IAsyncProcessorRequestsApi api, IAsyncProcessingLogic<T> logic)
        {
            _api = api;
            _logic = logic;
        }

        public async Task Consume(MqMessage<QueueRequestMessage> message)
        {
            await _api.MakeRequestProcessing(message.Payload.Id);

            var request = JsonConvert.DeserializeObject<T>(message.Payload.Content);

            var procOperator = new ProcessingOperator(message.Payload.Id, _api);

            try
            {
                await _logic.ProcessAsync(request, procOperator);
            }
            catch (Exception e)
            {
                await _api.CompleteWithErrorAsync(message.Payload.Id, new ProcessingError
                {
                    TechMessage = e.Message,
                    TechInfo = e.ToString()
                });
            }
        }
    }
}
