using OrderMatcher.Serializers;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace OrderMatcher.Tests
{
    public class MatchingEngineResultSerializerTests
    {
        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            var bytes = MatchingEngineResultSerializer.Serialize(new MatchingEngineResult { OrderId = UInt64.MinValue, Result = OrderMatchingResult.OrderAccepted, Timestamp = long.MinValue });
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            var bytes = MatchingEngineResultSerializer.Serialize(new MatchingEngineResult { OrderId = UInt64.MaxValue, Result = OrderMatchingResult.QuantityAndTotalQuantityShouldBeMultipleOfStepSize, Timestamp = long.MaxValue });
        }

        [Fact]
        public void Serialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => MatchingEngineResultSerializer.Serialize(null));
            Assert.Equal("matchingEngineResult", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => MatchingEngineResultSerializer.Deserialize(null));
            Assert.Equal("bytes", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsLessThan35Bytes()
        {
            var bytes = new byte[23];
            Exception ex = Assert.Throws<Exception>(() => MatchingEngineResultSerializer.Deserialize(bytes));
            Assert.Equal("OrderMatchingResult Message must be of Size : 24", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
        {
            var bytes = new byte[25];
            Exception ex = Assert.Throws<Exception>(() => MatchingEngineResultSerializer.Deserialize(bytes));
            Assert.Equal("OrderMatchingResult Message must be of Size : 24", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[24];
            Exception ex = Assert.Throws<Exception>(() => MatchingEngineResultSerializer.Deserialize(bytes));
            Assert.Equal("Invalid Message", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[24];
            bytes[4] = (byte)MessageType.OrderMatchingResult;
            Exception ex = Assert.Throws<Exception>(() => MatchingEngineResultSerializer.Deserialize(bytes));
            Assert.Equal("version mismatch", ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            var bytes = MatchingEngineResultSerializer.Serialize(new MatchingEngineResult { OrderId = UInt64.MinValue, Result = OrderMatchingResult.OrderAccepted, Timestamp = long.MinValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(24, messageLength);
            var result = MatchingEngineResultSerializer.Deserialize(bytes);
            Assert.Equal(ulong.MinValue, result.OrderId);
            Assert.Equal(OrderMatchingResult.OrderAccepted, result.Result);
            Assert.Equal(long.MinValue, result.Timestamp);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            var bytes = MatchingEngineResultSerializer.Serialize(new MatchingEngineResult { OrderId = UInt64.MaxValue, Result = OrderMatchingResult.QuantityAndTotalQuantityShouldBeMultipleOfStepSize, Timestamp = long.MaxValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(24, messageLength);
            var result = MatchingEngineResultSerializer.Deserialize(bytes);
            Assert.Equal(ulong.MaxValue, result.OrderId);
            Assert.Equal(OrderMatchingResult.QuantityAndTotalQuantityShouldBeMultipleOfStepSize, result.Result);
            Assert.Equal(long.MaxValue, result.Timestamp);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            var bytes = MatchingEngineResultSerializer.Serialize(new MatchingEngineResult { OrderId = 16879, Result = OrderMatchingResult.IcebergOrderCannotBeStopOrMarketOrder, Timestamp = 132465 });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(24, messageLength);
            var result = MatchingEngineResultSerializer.Deserialize(bytes);
            Assert.Equal((ulong)16879, result.OrderId);
            Assert.Equal(OrderMatchingResult.IcebergOrderCannotBeStopOrMarketOrder, result.Result);
            Assert.Equal(132465, result.Timestamp);
        }
    }
}
