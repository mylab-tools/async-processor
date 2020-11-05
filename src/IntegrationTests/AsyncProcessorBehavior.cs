using System;
using System.Threading.Tasks;
using IntegrationTest.Share;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MyLab.ApiClient;
using MyLab.ApiClient.Test;
using MyLab.AsyncProcessor.Api;
using MyLab.AsyncProcessor.Sdk;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.Mq;
using MyLab.Mq.Communication;
using MyLab.Mq.MqObjects;
using MyLab.Redis;
using TestProcessor;
using Xunit;
using Xunit.Abstractions;
using Startup = MyLab.AsyncProcessor.Api.Startup;

namespace IntegrationTests
{
    public class AsyncProcessorBehavior :
         IClassFixture<TestApi<Startup, IAsyncProcessorRequestsApi>>,
         IClassFixture<TestApi<TestProcessor.Startup, IProcessorApi>>
    {
        private readonly TestApi<Startup, IAsyncProcessorRequestsApi> _asyncProcTestApi;
        private readonly TestApi<TestProcessor.Startup, IProcessorApi> _procApi;
        private readonly ITestOutputHelper _output;

        static readonly MqOptions MqOptions = new MqOptions
        {
            Host = "localhost",
            Port = 10202,
            User = "guest",
            Password = "guest"
        };

        /// <summary>
        /// Initializes a new instance of <see cref="AsyncProcessorBehavior"/>
        /// </summary>
        public AsyncProcessorBehavior(
            TestApi<Startup, IAsyncProcessorRequestsApi> asyncProcTestApi,
            TestApi<TestProcessor.Startup, IProcessorApi> procApi,
            ITestOutputHelper output
            )
        {
            _asyncProcTestApi = asyncProcTestApi;
            _procApi = procApi;
            _output = output;
        }

        [Fact]
        public async Task ShouldProcessMessage()
        {
            //Arrange
            var queueName = "async-proc-test:queue:" + Guid.NewGuid().ToString("N");
            
            var asyncProcApi = StartAsyncProcApi(queueName);
            var processorApi = StartProcessor(queueName);

            var queue = CreateQueue(queueName);

            var request = new CreateRequest
            {
                Content = new TestRequest
                {
                    Value1 = "foo",
                    Value2 = 10,
                    Command = "concat"
                }
            };

            //Act

            asyncProcApi.Call(s => s.CreateAsync(request))

            //Assert

        }

        private MqQueue CreateQueue(string queueName)
        {
            var queueFactory = new MqQueueFactory(new DefaultMqConnectionProvider(MqOptions))
            {
                AutoDelete = true
            };

            return queueFactory.CreateWithName(queueName);
        }

        private TestApiClient<IProcessorApi> StartProcessor(string queueName)
        {
            var tc = _procApi.Start(srv =>
            {
                srv.Configure<MqOptions>(opt =>
                {
                    opt.Host = MqOptions.Host;
                    opt.Port = MqOptions.Port;
                    opt.User = MqOptions.User;
                    opt.Password = MqOptions.Password;
                });
            });

            tc.Output = _output;

            return tc;
        }

        private TestApiClient<IAsyncProcessorRequestsApi> StartAsyncProcApi(string queueName)
        {
            var tc=  _asyncProcTestApi.Start(srv =>
            {
                srv.Configure<MqOptions>(opt =>
                {
                    opt.Host = MqOptions.Host;
                    opt.Port = MqOptions.Port;
                    opt.User = MqOptions.User;
                    opt.Password = MqOptions.Password;
                });

                srv.Configure<RedisOptions>(opt =>
                {
                    opt.ConnectionString = "localhost:1201,allowAdmin=true";
                });

                srv.Configure<AsyncProcessorOptions>(opt =>
                {
                    opt.MaxIdleTime = TimeSpan.FromMinutes(5);
                    opt.QueueRoutingKey = queueName;
                    opt.MaxStoreTime = TimeSpan.FromMinutes(5);
                    opt.RedisKeyPrefix = "async-proc-test:" + Guid.NewGuid().ToString("N");
                } );
            });

            tc.Output = _output;

            return tc;
        }

        
    }
}
