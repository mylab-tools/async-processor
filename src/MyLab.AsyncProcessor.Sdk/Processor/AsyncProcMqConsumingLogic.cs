using System;
using System.Net.Http;
using System.Threading.Tasks;
using MyLab.ApiClient;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.Mq;
using MyLab.Mq.PubSub;
using Newtonsoft.Json;

namespace MyLab.AsyncProcessor.Sdk.Processor
{
    class AsyncProcMqConsumingLogic<T> : IMqConsumerLogic<QueueRequestMessage>
    {
        private readonly IAsyncProcessingLogic<T> _logic;
        private readonly IWebCallReporter _reporter;
        private readonly ApiClient<IAsyncProcessorRequestsApi> _api;

        public AsyncProcMqConsumingLogic(
            IHttpClientFactory httpClientFactory,
            IAsyncProcessingLogic<T> logic,
            IWebCallReporterFactory reporterFactory = null)
        {
            _logic = logic;
            _reporter = reporterFactory?.Create<IAsyncProcessorRequestsApi>();
            _api = httpClientFactory.CreateApiClient<IAsyncProcessorRequestsApi>();
        }

        public async Task Consume(MqMessage<QueueRequestMessage> message)
        {
            var processingReqDetails = await _api.Call(s => s.MakeRequestProcessing(message.Payload.Id)).GetDetailed(); 

            _reporter?.Report(processingReqDetails);

            var request = JsonConvert.DeserializeObject<T>(message.Payload.Content);

            var procOperator = new ProcessingOperator(message.Payload.Id, _api)
            {
                Reporter = _reporter
            };

            try
            {
                await _logic.ProcessAsync(request, procOperator);
            }
            catch (InterruptConsumingException)
            {
                throw;
            }
            catch (Exception e)
            {
                var completeWithErrorReqDetails = await _api.Call(s => s.CompleteWithErrorAsync(message.Payload.Id, new ProcessingError
                {
                    TechMessage = e.Message,
                    TechInfo = e.ToString()
                })).GetDetailed();

                _reporter?.Report(completeWithErrorReqDetails);
            }
        }
    }
}
