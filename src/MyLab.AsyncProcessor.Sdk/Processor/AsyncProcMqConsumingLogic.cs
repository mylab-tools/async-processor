using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyLab.ApiClient;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.Log.Dsl;
using MyLab.RabbitClient.Consuming;
using Newtonsoft.Json;

namespace MyLab.AsyncProcessor.Sdk.Processor
{
    class AsyncProcMqConsumingLogic<T> : RabbitConsumer<QueueRequestMessage>
    {
        private readonly IAsyncProcessingLogic<T> _logic;
        private readonly ILostRequestEventHandler _lostRequestEventHandler;
        private readonly IWebCallReporter _reporter;
        private readonly ApiClient<IAsyncProcessorRequestsApiV2> _api;
        private readonly IDslLogger _log;

        public AsyncProcMqConsumingLogic(
            IApiClientFactory httpClientFactory,
            IAsyncProcessingLogic<T> logic,
            IWebCallReporterFactory reporterFactory = null,
            ILostRequestEventHandler lostRequestEventHandler = null,
            ILogger<AsyncProcMqConsumingLogic<T>> logger = null)
        {
            _logic = logic;
            _lostRequestEventHandler = lostRequestEventHandler;
            _reporter = reporterFactory?.Create<IAsyncProcessorRequestsApiV2>();
            _api = httpClientFactory.CreateApiClient<IAsyncProcessorRequestsApiV2>();
            _log = logger?.Dsl();
        }

        protected override async Task ConsumeMessageAsync(ConsumedMessage<QueueRequestMessage> message)
        {
            var processingReqDetails = await _api.Request(s => s.MakeRequestProcessing(message.Content.Id)).GetDetailedAsync();

            _reporter?.Report(processingReqDetails);

            if (processingReqDetails.StatusCode == HttpStatusCode.NotFound)
            {
                _lostRequestEventHandler.Handle(message.Content.Id);
                return;
            }

            var requestContent = JsonConvert.DeserializeObject<T>(message.Content.Content);

            var request = new AsyncProcRequest<T>(message.Content.Id, message.Content.IncomingDt, requestContent, message.Content.Headers);

            var procOperator = new ProcessingOperator(message.Content.Id, _api)
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
                var completeWithErrorReqDetails = await _api.Request(s => s.CompleteWithErrorAsync(message.Content.Id, new ProcessingError
                {
                    TechMessage = e.Message,
                    TechInfo = e.ToString()
                })).GetDetailedAsync();

                _reporter?.Report(completeWithErrorReqDetails);
            }
        }
    }
}
