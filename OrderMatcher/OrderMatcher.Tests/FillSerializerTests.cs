using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class FillSerializerTests
    {
        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MinValue, TakerOrderId = OrderId.MinValue, Timestamp = long.MinValue, MatchQuantity = int.MinValue, MatchRate = int.MinValue, IncomingOrderFilled = true });
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MaxValue, TakerOrderId = OrderId.MaxValue, Timestamp = long.MaxValue, MatchQuantity = int.MaxValue, MatchRate = int.MaxValue, IncomingOrderFilled = false });
        }

        [Fact]
        public void Serialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => FillSerializer.Serialize(null));
            Assert.Equal("fill", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => FillSerializer.Deserialize(null));
            Assert.Equal("bytes", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsLessThan35Bytes()
        {
            var bytes = new byte[63];
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("Fill Message must be of Size : 64", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
        {
            var bytes = new byte[65];
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("Fill Message must be of Size : 64", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[64];
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("Invalid Message", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[64];
            bytes[4] = (byte)MessageType.Fill;
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("version mismatch", ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MinValue, TakerOrderId = OrderId.MinValue, Timestamp = long.MinValue, MatchQuantity = int.MinValue, MatchRate = int.MinValue, IncomingOrderFilled = false });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(64, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MinValue, fill.MakerOrderId);
            Assert.Equal(OrderId.MinValue, fill.TakerOrderId);
            Assert.Equal((Price)int.MinValue, fill.MatchRate);
            Assert.Equal((Quantity)int.MinValue, fill.MatchQuantity);
            Assert.Equal(long.MinValue, fill.Timestamp);
            Assert.False(fill.IncomingOrderFilled);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MaxValue, TakerOrderId = OrderId.MaxValue, Timestamp = long.MaxValue, MatchQuantity = int.MaxValue, MatchRate = int.MaxValue, IncomingOrderFilled = true });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(64, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MaxValue, fill.MakerOrderId);
            Assert.Equal(OrderId.MaxValue, fill.TakerOrderId);
            Assert.Equal((Price)int.MaxValue, fill.MatchRate);
            Assert.Equal((Quantity)int.MaxValue, fill.MatchQuantity);
            Assert.Equal(long.MaxValue, fill.Timestamp);
            Assert.True(fill.IncomingOrderFilled);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = 12345678, TakerOrderId = 56789, Timestamp = 404, MatchQuantity = 2356, MatchRate = 9534, IncomingOrderFilled = true });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(64, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, fill.MakerOrderId);
            Assert.Equal((OrderId)56789, fill.TakerOrderId);
            Assert.Equal((Price)9534, fill.MatchRate);
            Assert.Equal((Quantity)2356, fill.MatchQuantity);
            Assert.Equal(404, fill.Timestamp);
            Assert.True(fill.IncomingOrderFilled);
        }
    }
}
