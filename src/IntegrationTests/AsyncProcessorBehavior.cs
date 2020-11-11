using System;
using System.Text;
using System.Threading.Tasks;
using IntegrationTest.Share;
using MyLab.ApiClient.Test;
using MyLab.AsyncProcessor.Sdk;
using MyLab.AsyncProcessor.Sdk.DataModel;
using MyLab.AsyncProcessor.Sdk.Processor;
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
        public async Task ShouldProcessMessageWithObjectResult()
        {
            //Arrange
            var api = Prepare();

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
        }

        [Fact]
        public async Task ShouldProcessMessageWithIntResult()
        {
            //Arrange
            var api = Prepare();

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
        }

        [Fact]
        public async Task ShouldProcessMessageWithBinResult()
        {
            //Arrange
            var api = Prepare();

            var requestContent = new TestRequest
            {
                Value1 = "foo",
                Command = "str-to-bin"
            };

            //Act

            var reqId = await SendRequest(requestContent, api);
            var status = await ProcessRequest(reqId, api);
            var result = await GetResult(api, rApi => rApi.GetBinResult(reqId));

            //Assert
            Assert.Equal(ProcessStep.Completed, status.Step);
            Assert.True(status.Successful);
            Assert.Equal(Encoding.UTF8.GetBytes("foo"), result);
        }

        [Fact]
        public async Task ShouldProvideProcessingError()
        {
            //Arrange
            var api = Prepare();

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
        }

        [Fact]
        public async Task ShouldProvideReportedError()
        {
            //Arrange
            var api = Prepare();

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
        }

        [Fact]
        public async Task ShouldProvideConsumingError()
        {
            //Arrange
            var api = Prepare();

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
        }

        [Fact]
        public async Task ShouldProvideBizStep()
        {
            //Arrange
            var api = Prepare();

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
    }
}
