﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyLab.ApiClient;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.Log.Dsl;
using MyLab.Mq;
using MyLab.Mq.PubSub;
using Newtonsoft.Json;

namespace MyLab.AsyncProcessor.Sdk.Processor
{
    class AsyncProcMqConsumingLogic<T> : IMqConsumerLogic<QueueRequestMessage>
    {
        private readonly IAsyncProcessingLogic<T> _logic;
        private readonly ILostRequestEventHandler _lostRequestEventHandler;
        private readonly IWebCallReporter _reporter;
        private readonly ApiClient<IAsyncProcessorRequestsApi> _api;
        private readonly IDslLogger _log;

        public AsyncProcMqConsumingLogic(
            IHttpClientFactory httpClientFactory,
            IAsyncProcessingLogic<T> logic,
            IWebCallReporterFactory reporterFactory = null,
            ILostRequestEventHandler lostRequestEventHandler = null,
            ILogger<AsyncProcMqConsumingLogic<T>> logger = null)
        {
            _logic = logic;
            _lostRequestEventHandler = lostRequestEventHandler;
            _reporter = reporterFactory?.Create<IAsyncProcessorRequestsApi>();
            _api = httpClientFactory.CreateApiClient<IAsyncProcessorRequestsApi>();
            _log = logger?.Dsl();
        }

        public async Task Consume(MqMessage<QueueRequestMessage> message)
        {
            var processingReqDetails = await _api.Request(s => s.MakeRequestProcessing(message.Payload.Id)).GetDetailedAsync(); 

            _reporter?.Report(processingReqDetails);

            if (processingReqDetails.StatusCode == HttpStatusCode.NotFound)
            {
                _lostRequestEventHandler.Handle(message.Payload.Id);
                return;
            }

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
                var completeWithErrorReqDetails = await _api.Request(s => s.CompleteWithErrorAsync(message.Payload.Id, new ProcessingError
                {
                    TechMessage = e.Message,
                    TechInfo = e.ToString()
                })).GetDetailedAsync();

                _reporter?.Report(completeWithErrorReqDetails);
            }
        }
    }
}
