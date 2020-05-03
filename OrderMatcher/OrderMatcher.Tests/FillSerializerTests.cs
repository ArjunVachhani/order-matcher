using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class FillSerializerTests
    {
        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MinValue, TakerOrderId = OrderId.MinValue, Timestamp = int.MinValue, MatchQuantity = int.MinValue, MatchRate = int.MinValue, BidCost = Quantity.MinValue, AskRemainingQuantity = Quantity.MinValue });
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MaxValue, TakerOrderId = OrderId.MaxValue, Timestamp = int.MaxValue, MatchQuantity = int.MaxValue, MatchRate = int.MaxValue, BidCost = Quantity.MaxValue, AskRemainingQuantity = Quantity.MaxValue });
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
            var bytes = new byte[92];
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("Fill Message must be of Size : 93", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
        {
            var bytes = new byte[94];
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("Fill Message must be of Size : 93", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[93];
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("Invalid Message", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[93];
            bytes[4] = (byte)MessageType.Fill;
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("version mismatch", ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MinValue, TakerOrderId = OrderId.MinValue, Timestamp = int.MinValue, MatchQuantity = int.MinValue, MatchRate = int.MinValue, BidCost = Quantity.MinValue, AskRemainingQuantity = Quantity.MinValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(93, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MinValue, fill.MakerOrderId);
            Assert.Equal(OrderId.MinValue, fill.TakerOrderId);
            Assert.Equal(int.MinValue, fill.MatchRate);
            Assert.Equal(int.MinValue, fill.MatchQuantity);
            Assert.Equal(int.MinValue, fill.Timestamp);
            Assert.Equal(Quantity.MinValue, fill.BidCost);
            Assert.Equal(Quantity.MinValue, fill.AskRemainingQuantity);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MaxValue, TakerOrderId = OrderId.MaxValue, Timestamp = int.MaxValue, MatchQuantity = int.MaxValue, MatchRate = int.MaxValue, BidCost = Quantity.MaxValue, AskRemainingQuantity = Quantity.MaxValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(93, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MaxValue, fill.MakerOrderId);
            Assert.Equal(OrderId.MaxValue, fill.TakerOrderId);
            Assert.Equal(int.MaxValue, fill.MatchRate);
            Assert.Equal(int.MaxValue, fill.MatchQuantity);
            Assert.Equal(int.MaxValue, fill.Timestamp);
            Assert.Equal(Quantity.MaxValue, fill.AskRemainingQuantity);
            Assert.Equal(Quantity.MaxValue, fill.BidCost);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = 12345678, TakerOrderId = 56789, Timestamp = 404, MatchQuantity = 2356, MatchRate = 9534, BidCost = 4347, AskRemainingQuantity = 87135 });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(93, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, fill.MakerOrderId);
            Assert.Equal((OrderId)56789, fill.TakerOrderId);
            Assert.Equal(9534, fill.MatchRate);
            Assert.Equal(2356, fill.MatchQuantity);
            Assert.Equal(404, fill.Timestamp);
            Assert.Equal(4347, fill.BidCost);
            Assert.Equal(87135, fill.AskRemainingQuantity);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_BidCostNull()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = 12345678, TakerOrderId = 56789, Timestamp = 404, MatchQuantity = 2356, MatchRate = 9534, BidCost = null, AskRemainingQuantity = 87135 });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(93, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, fill.MakerOrderId);
            Assert.Equal((OrderId)56789, fill.TakerOrderId);
            Assert.Equal(9534, fill.MatchRate);
            Assert.Equal(2356, fill.MatchQuantity);
            Assert.Equal(404, fill.Timestamp);
            Assert.Null(fill.BidCost);
            Assert.Equal(87135, fill.AskRemainingQuantity);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_AskRemainingQuantityNull()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = 12345678, TakerOrderId = 56789, Timestamp = 404, MatchQuantity = 2356, MatchRate = 9534, BidCost = 4347, AskRemainingQuantity = null });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(93, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, fill.MakerOrderId);
            Assert.Equal((OrderId)56789, fill.TakerOrderId);
            Assert.Equal(9534, fill.MatchRate);
            Assert.Equal(2356, fill.MatchQuantity);
            Assert.Equal(404, fill.Timestamp);
            Assert.Equal(4347, fill.BidCost);
            Assert.Null(fill.AskRemainingQuantity);
        }
    }
}
