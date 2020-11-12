using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace UnitTests
{
    public class TimeSpanDeserializationBehavior
    {
        [Theory]
        [InlineData("0.01:00:00.000")]
        [InlineData("0.01:00:00")]
        [InlineData("01:00:00")]
        [InlineData("01:00")]
        public void ShouldDeserializeHour(string str)
        {
            //Arrange
            var expectedTs = TimeSpan.FromHours(1);

            //Act
            var actualTs = TimeSpan.Parse(str);

            //Assert
            Assert.Equal(expectedTs, actualTs);
        }

        [Fact]
        public void ShouldDeserializeDay()
        {
            //Arrange
            var expectedTs = TimeSpan.FromDays(1);

            //Act
            var actualTs = TimeSpan.Parse("1");

            //Assert
            Assert.Equal(expectedTs, actualTs);
        }
    }
}
