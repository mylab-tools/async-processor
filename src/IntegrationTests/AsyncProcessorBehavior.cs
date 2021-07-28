using System;
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
    public partial class AsyncProcessorBehavior :
         IClassFixture<TestApi<Startup, IAsyncProcessorRequestsApi>>,
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

                var reqId = await SendRequest(requestContent, api, predefinedRequestId);
                
                //Assert
                Assert.Equal(predefinedRequestId, reqId);
            }
            finally
            {
                callback.exchange.Dispose();
                callback.incomingMq.Dispose();
            }
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

                var reqId = await SendRequest(requestContent, api);
                var status = await ProcessRequest(reqId, api);
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
                callback.exchange.Dispose();
                callback.incomingMq.Dispose();
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

                var reqId = await SendRequest(requestContent, api);
                var status = await ProcessRequest(reqId, api);
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
                callback.exchange.Dispose();
                callback.incomingMq.Dispose();
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

                var reqId = await SendRequest(requestContent, api);
                var status = await ProcessRequest(reqId, api);
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
                callback.exchange.Dispose();
                callback.incomingMq.Dispose();
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

                var reqId = await SendRequest(requestContent, api);
                var status = await ProcessRequest(reqId, api);
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
                callback.exchange.Dispose();
                callback.incomingMq.Dispose();
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

                var reqId = await SendRequest(requestContent, api);
                var status = await ProcessRequest(reqId, api);

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
                callback.exchange.Dispose();
                callback.incomingMq.Dispose();
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

                var reqId = await SendRequest(requestContent, api);
                var status = await ProcessRequest(reqId, api);

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
                callback.exchange.Dispose();
                callback.incomingMq.Dispose();
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

                var reqId = await SendRequest(requestContent, api);

                RequestStatus status = await ProcessRequest(reqId, api);

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
                callback.exchange.Dispose();
                callback.incomingMq.Dispose();
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

                var reqId = await SendRequest(requestContent, api);

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
                callback.exchange.Dispose();
                callback.incomingMq.Dispose();
            }
        }
    }
}
