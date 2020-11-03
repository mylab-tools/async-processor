using System.Threading.Tasks;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.Mq;
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

            await _logic.Process(request, procOperator);
        }
    }
}
