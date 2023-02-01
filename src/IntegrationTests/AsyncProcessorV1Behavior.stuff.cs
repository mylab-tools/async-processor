using System;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IntegrationTest.Share;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using MyLab.ApiClient;
using MyLab.ApiClient.Test;
using MyLab.AsyncProcessor.Sdk;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.AsyncProcessor.Sdk.Processor;
using MyLab.Log;
using MyLab.Log.XUnit;
using MyLab.RabbitClient;
using MyLab.RabbitClient.Consuming;
using MyLab.RabbitClient.Model;
using MyLab.Redis;
using MyLab.Syslog;
using Newtonsoft.Json;
using TestProcessor;
using Xunit;
using Xunit.Abstractions;
using AsyncProcessorOptions = MyLab.AsyncProcessor.Api.AsyncProcessorOptions;
using Startup = MyLab.AsyncProcessor.Api.Startup;

namespace IntegrationTests
{
    public partial class AsyncProcessorV1Behavior
    {
        private readonly TestApi<Startup, IAsyncProcessorRequestsApiV1> _asyncProcTestApi;
        private readonly TestApi<TestProcessor.Startup, IProcessorApi> _procApi;
        private readonly ITestOutputHelper _output;

        

        /// <summary>
        /// Initializes a new instance of <see cref="AsyncProcessorV1Behavior"/>
        /// </summary>
        public AsyncProcessorV1Behavior(
            TestApi<Startup, IAsyncProcessorRequestsApiV1> asyncProcTestApi,
            TestApi<TestProcessor.Startup, IProcessorApi> procApi,
            ITestOutputHelper output
        )
        {
            _asyncProcTestApi = asyncProcTestApi;
            _procApi = procApi;
            _output = output;
        }

        (RabbitQueue incomingMq, RabbitExchange exchange) CreateCallback()
        {
            string exchName = "async-proc-test:callback:" + Guid.NewGuid().ToString("N");
            var exchange = TestTools.ExchangeFactory(autoDelete:false).CreateWithName(exchName);
            
            string queueName = "async-proc-test:callback:" + Guid.NewGuid().ToString("N");
            var queue = TestTools.QueueFactory(autoDelete:false).CreateWithName(queueName);

            queue.BindToExchange(exchange, "foo-callback");

            return (queue, exchange);
        }

        private (TestApiClient<IAsyncProcessorRequestsApiV1> AsyncProcApi, TestApiClient<IProcessorApi> ProcApi) Prepare(string callbackExchangeName, ILostRequestEventHandler lostRequestEventHandler = null, int reqIdleTimeoutSec = 300)
        {
            var deadLetterExchange = CreateDeadLetterExchange();
            var deadLetterQueue = CreateQueue(null, "async-proc-test:dead-letter:");
            deadLetterQueue.BindToExchange(deadLetterExchange);

            var queue = CreateQueue(deadLetterExchange);

            var asyncProcApi = StartAsyncProcApi(queue, deadLetterQueue, callbackExchangeName, reqIdleTimeoutSec, out var asyncProcApiInnerClient);
            var processorApi = StartProcessor(queue, asyncProcApiInnerClient, lostRequestEventHandler);

            return (asyncProcApi, processorApi);
        }

        private async Task<string> SendRequest(
            TestRequest request,
            TestApiClient<IAsyncProcessorRequestsApiV1> api,
            string predefinedId = null)
        {
            var createRequest = new CreateRequest
            {
                RequestId = predefinedId,
                Content = SerializeRequest(request),
                CallbackRouting = "foo-callback"
            };

            var reqIdResp = await api.Call(s => s.CreateAsync(createRequest, null));
            return reqIdResp.ResponseContent;
        }

        private async Task<RequestStatus> ProcessRequestAsync(
            string reqId,
            TestApiClient<IAsyncProcessorRequestsApiV1> api)
        {
            TestCallDetails<RequestStatus> statusResp = null;
            
            for (int i = 0; i < 10; i++)
            {
                if (i != 0)
                    await Task.Delay(200);

                statusResp = await api.Call(s => s.GetStatusAsync(reqId));

                if (statusResp.StatusCode == HttpStatusCode.NotFound)
                    throw new FileNotFoundException();

                if (statusResp.ResponseContent.Step == ProcessStep.Completed)
                    break;
            }

            if (statusResp == null || statusResp.ResponseContent.Step != ProcessStep.Completed)
                throw new TimeoutException("Waiting for response timeout");

            return statusResp.ResponseContent;
        }

        private async Task<T> GetResult<T>(
            (TestApiClient<IAsyncProcessorRequestsApiV1> AsyncProcApi, TestApiClient<IProcessorApi> ProcApi) api,
            Expression<Func<IAsyncProcessorRequestsApiV1, Task<T>>> call)
        {
            var resResp = await api.AsyncProcApi.Call(call);

            return resResp.ResponseContent;
        }

        private string SerializeRequest(TestRequest request)
        {
            return JsonConvert.SerializeObject(request);
        }

        private RabbitQueue CreateQueue(RabbitExchange deadLetterExchange, string prefix = null)
        {
            var queueFactory = TestTools.QueueFactory();
            queueFactory.AutoDelete = true;
            queueFactory.DeadLetterExchange = deadLetterExchange?.Name;

            string name = (prefix ?? "async-proc-test:queue:") + Guid.NewGuid().ToString("N");
            return queueFactory.CreateWithName(name);
        }

        private RabbitExchange CreateDeadLetterExchange()
        {
            var exchangeFactory = TestTools.ExchangeFactory(RabbitExchangeType.Fanout);
            exchangeFactory.AutoDelete = true;

            string name = "async-proc-test:dead-letter:" + Guid.NewGuid().ToString("N") + ":dead-letter";
            return exchangeFactory.CreateWithName(name);
        }

        private TestApiClient<IProcessorApi> StartProcessor(RabbitQueue queue, HttpClient asyncProcApiClient,
            ILostRequestEventHandler lostRequestEventHandler = null,
            IAsyncProcessingLogic<TestRequest> processorLogic = null)
        {
            var tc = _procApi.Start(srv =>
            {
                srv.Configure<RabbitOptions>(opt =>
                {
                    opt.Host = TestTools.MqOptions.Host;
                    opt.Port = TestTools.MqOptions.Port;
                    opt.User = TestTools.MqOptions.User;
                    opt.Password = TestTools.MqOptions.Password;
                });

                srv.Configure<MyLab.AsyncProcessor.Sdk.Processor.AsyncProcessorOptions>(opt =>
                {
                    opt.Queue = queue.Name;
                });

                srv.AddApiClients(reg => { reg.RegisterContract<IAsyncProcessorRequestsApiV1>(); },
                    new SingleHttpClientFactory(asyncProcApiClient));

                srv.AddLogging(l => l
                    .AddFilter(lvl => true)
                    .AddMyLabConsole()
                    .AddXUnit(_output)
                );

                srv.AddSingleton<IWebCallReporterFactory>(new WebCallReporterFactory(_output));

                if (lostRequestEventHandler != null)
                {
                    srv.AddSingleton(lostRequestEventHandler);
                }

                if (processorLogic != null)
                {
                    srv.AddSingleton<IAsyncProcessingLogic<TestRequest>>(processorLogic);
                }
            });


            tc.Output = _output;

            return tc;
        }

        private TestApiClient<IAsyncProcessorRequestsApiV1> StartAsyncProcApi(
            RabbitQueue queue,
            RabbitQueue deadLetterQueue,
            string callbackExchangeName,
            int reqProcTimeoutSec,
            out HttpClient innerHttpClient)
        {
            var tc = _asyncProcTestApi.Start(out innerHttpClient, srv =>
            {
                srv.Configure<SyslogLoggerOptions>(opt =>
                {
                    opt.RemoteHost = "localhost";
                    opt.RemotePort = 123456;
                });

                srv.Configure<RabbitOptions>(opt =>
                {
                    opt.Host = TestTools.MqOptions.Host;
                    opt.Port = TestTools.MqOptions.Port;
                    opt.User = TestTools.MqOptions.User;
                    opt.Password = TestTools.MqOptions.Password;
                });

                srv.Configure<RedisOptions>(opt => { opt.ConnectionString = "localhost:10201,allowAdmin=true"; });

                srv.Configure<AsyncProcessorOptions>(opt =>
                {
                    opt.ProcessingTimeout = TimeSpan.FromSeconds(reqProcTimeoutSec);
                    opt.QueueRoutingKey = queue.Name;
                    opt.RestTimeout = TimeSpan.FromMinutes(5);
                    opt.RedisKeyPrefix = "async-proc-test:" + Guid.NewGuid().ToString("N");
                    opt.DeadLetter = deadLetterQueue.Name;
                    opt.Callback = callbackExchangeName;
                });

                srv.AddLogging(l => l
                    .AddFilter(lvl => true)
                    .AddMyLabConsole()
                    .AddXUnit(_output)
                );
                //.Configure<ConsoleFormatterOptions>(opt => opt.IncludeScopes = true);

            });

            tc.Output = _output;

            return tc;
        }

        private class SingleHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _client;

            public SingleHttpClientFactory(HttpClient client)
            {
                _client = client;
            }

            public HttpClient CreateClient(string name)
            {
                return _client;
            }
        }

        async Task AssertCallback(RabbitQueue callbackQueue, Action<ChangeStatusCallbackMessage>[] asserts)
        {
            await Task.Delay(500);

            for (int i = 0; i < asserts.Length; i++)
            {
                var assert = asserts[i];

                try
                {
                    var rm = callbackQueue.Listen<ChangeStatusCallbackMessage>(TimeSpan.FromSeconds(5));
                    
                    Assert.NotNull(rm);

                    assert(rm.Content);
                }
                catch (TimeoutException)
                {
                    Assert.True(false, $"Callback msg #'{i}' read timeout");
                }
            }
        }

        class TestLostRequestEventHandler : ILostRequestEventHandler
        {
            public string LastLostRequestId { get; set; }

            public void Handle(string reqId)
            {
                LastLostRequestId = reqId;
            }
        }

        class TestProcessingLogic : IAsyncProcessingLogic<TestRequest>
        {
            public AsyncProcRequest<TestRequest> LastRequest { get; set; }
            public Task ProcessAsync(AsyncProcRequest<TestRequest> request, IProcessingOperator op)
            {
                LastRequest = request;

                return Task.CompletedTask;
            }
        }
    }
}