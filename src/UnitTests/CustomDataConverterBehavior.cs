using MyLab.AsyncProcessor.Sdk;
using MyLab.AsyncProcessor.Sdk.DataModel;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    public class CustomDataConverterBehavior
    {
        private readonly ITestOutputHelper _output;

        public CustomDataConverterBehavior(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ShouldDeserializeCustomData()
        {
            //Arrange
            var dataJson = "{\"property\":\"value\"}";
            var json = "{\"routing\":null,\"content\":" + dataJson + "}";

            //Act
            var obj = JsonConvert.DeserializeObject<CreateRequest>(json);

            //Assert
            Assert.NotNull(obj);
            Assert.Equal(dataJson, obj.Content);
        }

        [Fact]
        public void ShouldSerializeCustomData()
        {
            //Arrange
            var dataJson = "{\"property\":\"value\"}";
            var obj = new CreateRequest
            {
                Content = dataJson
            };

            //Act
            var json = JsonConvert.SerializeObject(obj);

            _output.WriteLine("Json: " + json);

            //Assert
            Assert.NotNull(json);
            Assert.Equal("{\"routing\":null,\"content\":" + dataJson + "}", json);
        }
    }
}
