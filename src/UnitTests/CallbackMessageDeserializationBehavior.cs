using System;
using System.Collections.Generic;
using System.Text;
using MyLab.ApiClient;
using MyLab.AsyncProcessor.Sdk.DataModel;
using Newtonsoft.Json;
using Xunit;

namespace UnitTests
{
    public class CallbackMessageDeserializationBehavior
    {
        [Fact]
        public void ShouldDeserialize()
        {
            //Arrange
            string json = "{\"reqId\":\"d58f3c18c1e744b0b25388395fe35dc5\",\"newProcStep\":3,\"resObj\":\"foo-10\"}";
           
            //Act
            var res = JsonConvert.DeserializeObject<ChangeStatusCallbackMessage>(json);

            //Assert
            Assert.NotNull(res);
            Assert.Equal("d58f3c18c1e744b0b25388395fe35dc5", res.RequestId);
            Assert.Equal(ProcessStep.Completed, res.NewProcessStep);
            Assert.Equal("foo-10", res.ResultObjectJson);
        }
        
        [Fact]
        public void ShouldDeserializeWithBin()
        {
            //Arrange
            string json = "{\"reqId\":\"4ef8f5e6c86e40a0a2e2c89d63e4aed2\",\"newProcStep\":3,\"resBin\":\"Zm9v\"}";

            var expectedBin = Convert.ToBase64String(Encoding.UTF8.GetBytes("foo"));

            //Act
            var res = JsonConvert.DeserializeObject<ChangeStatusCallbackMessage>(json);

            //Assert
            Assert.NotNull(res);
            Assert.Equal("4ef8f5e6c86e40a0a2e2c89d63e4aed2", res.RequestId);
            Assert.Equal(ProcessStep.Completed, res.NewProcessStep);
            Assert.Equal(expectedBin, Convert.ToBase64String(res.ResultBin));
        }
    }
}
