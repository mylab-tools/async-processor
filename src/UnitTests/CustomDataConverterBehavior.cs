using MyLab.AsyncProcessor.Api.DataModel;
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
            var json = "{\"routing\":null,\"data\":" + dataJson + "}";

            //Act
            var obj = JsonConvert.DeserializeObject<CreateRequest>(json);

            //Assert
            Assert.NotNull(obj);
            Assert.Equal(dataJson, obj.Data);
        }

        [Fact]
        public void ShouldSerializeCustomData()
        {
            //Arrange
            var dataJson = "{\"property\":\"value\"}";
            var obj = new CreateRequest
            {
                Data = dataJson
            };

            //Act
            var json = JsonConvert.SerializeObject(obj);

            _output.WriteLine("Json: " + json);

            //Assert
            Assert.NotNull(json);
            Assert.Equal("{\"routing\":null,\"data\":" + dataJson + "}", json);
        }
    }
}
