using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class FillSerializerTests
    {
        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = ulong.MinValue, TakerOrderId = ulong.MinValue, Timestamp = long.MinValue, MatchQuantity = int.MinValue, MatchRate = int.MinValue });
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = ulong.MaxValue, TakerOrderId = ulong.MaxValue, Timestamp = long.MaxValue, MatchQuantity = int.MaxValue, MatchRate = int.MaxValue });
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
            var bytes = new byte[38];
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("Fill Message must be of Size : 39", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
        {
            var bytes = new byte[40];
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("Fill Message must be of Size : 39", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[39];
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("Invalid Message", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[39];
            bytes[4] = (byte)MessageType.Fill;
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("version mismatch", ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = ulong.MinValue, TakerOrderId = ulong.MinValue, Timestamp = long.MinValue, MatchQuantity = int.MinValue, MatchRate = int.MinValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(39, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal(ulong.MinValue, fill.MakerOrderId);
            Assert.Equal(ulong.MinValue, fill.TakerOrderId);
            Assert.Equal((Price)int.MinValue, fill.MatchRate);
            Assert.Equal((Quantity)int.MinValue, fill.MatchQuantity);
            Assert.Equal(long.MinValue, fill.Timestamp);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = ulong.MaxValue, TakerOrderId = ulong.MaxValue, Timestamp = long.MaxValue, MatchQuantity = int.MaxValue, MatchRate = int.MaxValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(39, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal(ulong.MaxValue, fill.MakerOrderId);
            Assert.Equal(ulong.MaxValue, fill.TakerOrderId);
            Assert.Equal((Price)int.MaxValue, fill.MatchRate);
            Assert.Equal((Quantity)int.MaxValue, fill.MatchQuantity);
            Assert.Equal(long.MaxValue, fill.Timestamp);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = 12345678, TakerOrderId = 56789, Timestamp = 404, MatchQuantity = 2356, MatchRate = 9534 });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(39, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal((ulong)12345678, fill.MakerOrderId);
            Assert.Equal((ulong)56789, fill.TakerOrderId);
            Assert.Equal((Price)9534, fill.MatchRate);
            Assert.Equal((Quantity)2356, fill.MatchQuantity);
            Assert.Equal(404, fill.Timestamp);
        }
    }
}
