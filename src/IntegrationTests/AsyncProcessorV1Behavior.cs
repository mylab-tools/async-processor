using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IntegrationTest.Share;
using MyLab.ApiClient.Test;
using MyLab.AsyncProcessor.Sdk;
using MyLab.AsyncProcessor.Sdk.DataModel;
using TestProcessor;
using Xunit;
using Startup = MyLab.AsyncProcessor.Api.Startup;

namespace IntegrationTests
{
    public partial class AsyncProcessorV1Behavior :
         IClassFixture<TestApi<Startup, IAsyncProcessorRequestsApiV1>>,
         IClassFixture<TestApi<TestProcessor.Startup, IProcessorApi>>
    {
        [Fact]
        public async Task ShouldAssignPredefinedRequestId()
        {
            //Arrange

            var callback = CreateCallback();
            string predefinedRequestId = Guid.NewGuid().ToString("N");

            try
            {
                var api = Prepare(callback.exchange.Name);

                var requestContent = new TestRequest
                {
                    Value1 = "foo",
                    Value2 = 10,
                    Command = "concat"
                };

                //Act

                var reqId = await SendRequest(requestContent, api.AsyncProcApi, predefinedRequestId);
                
                //Assert
                Assert.Equal(predefinedRequestId, reqId);
            }
            finally
            {
                callback.exchange.Remove();
                callback.incomingMq.Remove();
            }
        }

        [Fact]
        public async Task ShouldProcessingIfRequestNotExpired()
        {
            //Arrange

            var callback = CreateCallback();
            string reqId;
            RequestStatus reqStatResult;

            try
            {
                var deadLetterExchange = CreateDeadLetterExchange();
                var deadLetterQueue = CreateQueue(null, "async-proc-test:dead-letter:");
                deadLetterQueue.BindToExchange(deadLetterExchange);

                var queue = CreateQueue(deadLetterExchange);

                var asyncProcApi = StartAsyncProcApi(queue, deadLetterQueue, callback.exchange.Name, 3, out var asyncProcApiInnerClient);
                
                var requestContent = new TestRequest
                {
                    Value1 = "foo",
                    Value2 = 10,
                    Command = "concat"
                };

                //Act

                reqId = await SendRequest(requestContent, asyncProcApi);

                await Task.Delay(TimeSpan.FromSeconds(1));

                StartProcessor(queue, asyncProcApiInnerClient);

                reqStatResult = await ProcessRequestAsync(reqId, asyncProcApi);
            }
            finally
            {
                callback.exchange.Remove();
                callback.incomingMq.Remove();
            }

            Assert.NotNull(reqStatResult);
            Assert.True(reqStatResult.Successful);
        }

        [Fact]
        public async Task ShouldExpireRequest()
        {
            //Arrange

            var callback = CreateCallback();
            var testProcLogic = new TestProcessingLogic();
            var testLostRequestHandler = new TestLostRequestEventHandler();
            string reqId;

            try
            {
                var deadLetterExchange = CreateDeadLetterExchange();
                var deadLetterQueue = CreateQueue(null, "async-proc-test:dead-letter:");
                deadLetterQueue.BindToExchange(deadLetterExchange);

                var queue = CreateQueue(deadLetterExchange);

                var asyncProcApi = StartAsyncProcApi(queue, deadLetterQueue, callback.exchange.Name, 1, out var asyncProcApiInnerClient);

                var requestContent = new TestRequest
                {
                    Value1 = "foo",
                    Value2 = 10,
                    Command = "concat"
                };

                //Act

                reqId = await SendRequest(requestContent, asyncProcApi);

                await Task.Delay(TimeSpan.FromSeconds(2));

                StartProcessor(queue, asyncProcApiInnerClient, testLostRequestHandler, testProcLogic);

                //Act & Assert
                await Assert.ThrowsAsync<FileNotFoundException>(() => ProcessRequestAsync(reqId, asyncProcApi));
            }
            finally
            {
                callback.exchange.Remove();
                callback.incomingMq.Remove();
            }

            Assert.Null(testProcLogic.LastRequest);
        }

        [Fact]
        public async Task ShouldAddInitialHeadersToRequest()
        {
            //Arrange

            var callback = CreateCallback();
            var testProcLogic = new TestProcessingLogic();

            try
            {
                var deadLetterExchange = CreateDeadLetterExchange();
                var deadLetterQueue = CreateQueue(null, "async-proc-test:dead-letter:");
                deadLetterQueue.BindToExchange(deadLetterExchange);

                var queue = CreateQueue(deadLetterExchange);

                var asyncProcApi = StartAsyncProcApi(queue, deadLetterQueue, callback.exchange.Name, 3, out var asyncProcApiInnerClient);

                var requestContent = new TestRequest
                {
                    Value1 = "foo",
                    Value2 = 10,
                    Command = "concat"
                };

                //Act

                var reqId = await SendRequest(requestContent, asyncProcApi);

                StartProcessor(queue, asyncProcApiInnerClient, processorLogic: testProcLogic);

                await Assert.ThrowsAsync<TimeoutException>(() => ProcessRequestAsync(reqId, asyncProcApi));
            }
            finally
            {
                callback.exchange.Remove();
                callback.incomingMq.Remove();
            }

            var headers = testProcLogic?.LastRequest?.Headers;

            Assert.NotNull(headers);
            Assert.True(headers.ContainsKey("Content-Length"));
            Assert.Equal("128", headers["Content-Length"]);
            
        }

        [Fact]
        public async Task ShouldProcessMessageWithObjectResult()
        {
            //Arrange

            var callback = CreateCallback();

            try
            {
                var api = Prepare(callback.exchange.Name);

                var requestContent = new TestRequest
                {
                    Value1 = "foo",
                    Value2 = 10,
                    Command = "concat"
                };

                //Act

                var reqId = await SendRequest(requestContent, api.AsyncProcApi);
                var status = await ProcessRequestAsync(reqId, api.AsyncProcApi);
                var result = await GetResult(api, rApi => rApi.GetObjectResult<string>(reqId));

                //Assert
                Assert.Equal(ProcessStep.Completed, status.Step);
                Assert.True(status.Successful);
                Assert.Equal("foo-10", result);

                await AssertCallback(callback.incomingMq, new Action<ChangeStatusCallbackMessage>[]
                {
                    m => Assert.Equal(ProcessStep.Processing, m.NewProcessStep),
                    m =>
                    {
                        Assert.Equal(ProcessStep.Completed, m.NewProcessStep);
                        Assert.Equal("foo-10", m.ResultObjectJson);
                    }
                });
            }
            finally
            {
                callback.exchange.Remove();
                callback.incomingMq.Remove();
            }
        }

        [Fact]
        public async Task ShouldProcessMessageWithLargeObjectResult()
        {
            //Arrange

            var callback = CreateCallback();

            try
            {
                var api = Prepare(callback.exchange.Name);

                var requestContent = new TestRequest
                {
                    Command = "repeat-str",
                    Value1 = Guid.NewGuid().ToString("N"),
                    Value2 = 1000
                };

                var expected = string.Join(',', Enumerable.Repeat(requestContent.Value1, requestContent.Value2));

                //Act

                var reqId = await SendRequest(requestContent, api.AsyncProcApi);
                var status = await ProcessRequestAsync(reqId, api.AsyncProcApi);
                var result = await GetResult(api, rApi => rApi.GetObjectResult<string>(reqId));

                //Assert
                Assert.Equal(ProcessStep.Completed, status.Step);
                Assert.True(status.Successful);
                Assert.Equal(expected, result);

                await AssertCallback(callback.incomingMq, new Action<ChangeStatusCallbackMessage>[]
                {
                    m => Assert.Equal(ProcessStep.Processing, m.NewProcessStep),
                    m =>
                    {
                        Assert.Equal(ProcessStep.Completed, m.NewProcessStep);
                        Assert.Equal(expected, m.ResultObjectJson);
                    }
                });
            }
            finally
            {
                callback.exchange.Remove();
                callback.incomingMq.Remove();
            }
        }

        [Fact]
        public async Task ShouldProcessMessageWithIntResult()
        {
            //Arrange
            var callback = CreateCallback();
            try
            {
                var api = Prepare(callback.exchange.Name);

                var requestContent = new TestRequest
                {
                    Value2 = 10,
                    Command = "incr-int"
                };

                //Act

                var reqId = await SendRequest(requestContent, api.AsyncProcApi);
                var status = await ProcessRequestAsync(reqId, api.AsyncProcApi);
                var result = await GetResult(api, rApi => rApi.GetObjectResult<int>(reqId));

                //Assert
                Assert.Equal(ProcessStep.Completed, status.Step);
                Assert.True(status.Successful);
                Assert.Equal(11, result);

                await AssertCallback(callback.incomingMq, new Action<ChangeStatusCallbackMessage>[]
                {
                    m => Assert.Equal(ProcessStep.Processing, m.NewProcessStep),
                    m =>
                    {
                        Assert.Equal(ProcessStep.Completed, m.NewProcessStep);
                        Assert.Equal("11", m.ResultObjectJson);
                    }
                });
            }
            finally
            {
                callback.exchange.Remove();
                callback.incomingMq.Remove();
            }
        }

        [Fact]
        public async Task ShouldProcessMessageWithBinResult()
        {
            //Arrange
            var callback = CreateCallback();
            try
            {
                var api = Prepare(callback.exchange.Name);

                var requestContent = new TestRequest
                {
                    Value1 = "foo",
                    Command = "str-to-bin"
                };

                var expectedBin = Encoding.UTF8.GetBytes("foo");

                //Act

                var reqId = await SendRequest(requestContent, api.AsyncProcApi);
                var status = await ProcessRequestAsync(reqId, api.AsyncProcApi);
                var result = await GetResult(api, rApi => rApi.GetBinResult(reqId));

                //Assert
                Assert.Equal(ProcessStep.Completed, status.Step);
                Assert.True(status.Successful);
                Assert.Equal(expectedBin, result);

                await AssertCallback(callback.incomingMq, new Action<ChangeStatusCallbackMessage>[]
                {
                    m => Assert.Equal(ProcessStep.Processing, m.NewProcessStep),
                    m =>
                    {
                        Assert.Equal(ProcessStep.Completed, m.NewProcessStep);
                        Assert.Equal(Convert.ToBase64String(expectedBin), Convert.ToBase64String(m.ResultBin));
                    }
                });
            }
            finally
            {
                callback.exchange.Remove();
                callback.incomingMq.Remove();
            }
        }

        [Fact]
        public async Task ShouldProcessMessageWithoutResult()
        {
            //Arrange
            var callback = CreateCallback();
            try
            {
                var api = Prepare(callback.exchange.Name);

                var requestContent = new TestRequest
                {
                    Value1 = "foo",
                    Command = "simple-complete"
                };
                
                //Act

                var reqId = await SendRequest(requestContent, api.AsyncProcApi);
                var status = await ProcessRequestAsync(reqId, api.AsyncProcApi);
                var result = await GetResult(api, rApi => rApi.GetBinResult(reqId));

                //Assert
                Assert.Equal(ProcessStep.Completed, status.Step);
                Assert.True(status.Successful);

                await AssertCallback(callback.incomingMq, new Action<ChangeStatusCallbackMessage>[]
                {
                    m => Assert.Equal(ProcessStep.Processing, m.NewProcessStep),
                    m =>
                    {
                        Assert.Equal(ProcessStep.Completed, m.NewProcessStep);
                    }
                });
            }
            finally
            {
                callback.exchange.Remove();
                callback.incomingMq.Remove();
            }
        }

        [Fact]
        public async Task ShouldProvideProcessingError()
        {
            //Arrange
            var callback = CreateCallback();
            try
            {
                var api = Prepare(callback.exchange.Name);

                var requestContent = new TestRequest
                {
                    Command = "unhandled-exception"
                };

                //Act

                var reqId = await SendRequest(requestContent, api.AsyncProcApi);
                var status = await ProcessRequestAsync(reqId, api.AsyncProcApi);

                //Assert
                Assert.Equal(ProcessStep.Completed, status.Step);
                Assert.False(status.Successful);
                Assert.NotNull(status.Error);
                Assert.Equal("foo", status.Error.TechMessage);
                Assert.Null(status.Error.BizMessage);
                Assert.NotNull(status.Error.TechInfo);

                await AssertCallback(callback.incomingMq, new Action<ChangeStatusCallbackMessage>[]
                {
                    m => Assert.Equal(ProcessStep.Processing, m.NewProcessStep),
                    m =>
                    {
                        Assert.Equal(ProcessStep.Completed, m.NewProcessStep);
                        Assert.Null(m.ResultObjectJson);
                        Assert.NotNull(m.OccurredError);
                        Assert.Equal("foo", m.OccurredError.TechMessage);
                        Assert.NotNull(m.OccurredError.TechInfo);
                    }
                });
            }
            finally
            {
                callback.exchange.Remove();
                callback.incomingMq.Remove();
            }
        }

        [Fact]
        public async Task ShouldProvideReportedError()
        {
            //Arrange
            var callback = CreateCallback();
            try
            {
                var api = Prepare(callback.exchange.Name);

                var requestContent = new TestRequest
                {
                    Command = "reported-error"
                };

                //Act

                var reqId = await SendRequest(requestContent, api.AsyncProcApi);
                var status = await ProcessRequestAsync(reqId, api.AsyncProcApi);

                //Assert
                Assert.Equal(ProcessStep.Completed, status.Step);
                Assert.False(status.Successful);
                Assert.NotNull(status.Error);
                Assert.Equal("bar", status.Error.TechMessage);
                Assert.Equal("foo", status.Error.BizMessage);
                Assert.NotNull(status.Error.TechInfo);

                await AssertCallback(callback.incomingMq, new Action<ChangeStatusCallbackMessage>[]
                {
                    m => Assert.Equal(ProcessStep.Processing, m.NewProcessStep),
                    m =>
                    {
                        Assert.Equal(ProcessStep.Completed, m.NewProcessStep);
                        Assert.Null(m.ResultObjectJson);
                        Assert.NotNull(m.OccurredError);
                        Assert.Equal("bar", m.OccurredError.TechMessage);
                        Assert.Equal("foo", m.OccurredError.BizMessage);
                        Assert.NotNull(m.OccurredError.TechInfo);
                    }
                });
            }
            finally
            {
                callback.exchange.Remove();
                callback.incomingMq.Remove();
            }
        }

        [Fact]
        public async Task ShouldProvideConsumingError()
        {
            //Arrange
            var callback = CreateCallback();
            try
            {
                var api = Prepare(callback.exchange.Name);

                var requestContent = new TestRequest
                {
                    Command = "interrupt"
                };

                //Act

                var reqId = await SendRequest(requestContent, api.AsyncProcApi);
                RequestStatus status = await ProcessRequestAsync(reqId, api.AsyncProcApi);

                //Assert
                Assert.Equal(ProcessStep.Completed, status.Step);
                Assert.False(status.Successful);
                Assert.NotNull(status.Error);
                Assert.Equal("The request was placed in the dead letter", status.Error.TechMessage);
                Assert.Null(status.Error.BizMessage);
                Assert.Null(status.Error.TechInfo);

                await AssertCallback(callback.incomingMq, new Action<ChangeStatusCallbackMessage>[]
                {
                    m => Assert.Equal(ProcessStep.Processing, m.NewProcessStep),
                    m =>
                    {
                        Assert.Equal(ProcessStep.Completed, m.NewProcessStep);
                        Assert.Null(m.ResultObjectJson);
                        Assert.NotNull(m.OccurredError);
                        Assert.Equal("The request was placed in the dead letter", m.OccurredError.TechMessage);
                        Assert.Null(m.OccurredError.BizMessage);
                        Assert.Null(m.OccurredError.TechInfo);
                    }
                });
            }
            finally
            {
                callback.exchange.Remove();
                callback.incomingMq.Remove();
            }
        }

        [Fact]
        public async Task ShouldProvideBizStep()
        {
            //Arrange
            var callback = CreateCallback();
            try
            {
                var api = Prepare(callback.exchange.Name);

                var requestContent = new TestRequest
                {
                    Command = "biz-step"
                };

                //Act

                var reqId = await SendRequest(requestContent, api.AsyncProcApi);

                await api.ProcApi.Call(p => p.GetStatus());

                TestCallDetails<RequestStatus> statusResp = await api.AsyncProcApi.Call(s => s.GetStatusAsync(reqId));
                int tryCount = 0;

                while (statusResp.ResponseContent.Step != ProcessStep.Completed && tryCount++ < 10)
                {
                    await Task.Delay(500);
                    statusResp = await api.AsyncProcApi.Call(s => s.GetStatusAsync(reqId));
                }

                var status = statusResp.ResponseContent;

                //Assert
                Assert.Equal(ProcessStep.Processing, status.Step);
                Assert.Equal("foo-step", status.BizStep);
            }
            finally
            {
                callback.exchange.Remove();
                callback.incomingMq.Remove();
            }
        }
    }
}
