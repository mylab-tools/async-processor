using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MyLab.AsyncProcessor.Api.Tools;
using Xunit;

namespace UnitTests
{
    public class RequestResultBehavior
    {
        [Fact]
        public async Task ShouldProvideBinData()
        {
            //Arrange
            var testData = Convert.ToBase64String(Encoding.UTF8.GetBytes("foo"));
            var resp = new RequestResult("application/octet-stream", testData);
            string resultData = null;

            //Act
            var httpContent = resp.ToHttpContent();
            if (httpContent != null)
            {
                var bytes = await httpContent.ReadAsByteArrayAsync();
                resultData = Encoding.UTF8.GetString(bytes);
            }

            //Assert
            Assert.Equal("foo", resultData);
        }
    }
}
