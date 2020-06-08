using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class FillSerializerTests
    {
        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MinValue, TakerOrderId = OrderId.MinValue, Timestamp = int.MinValue, MatchQuantity = int.MinValue, MatchRate = int.MinValue, BidCost = Quantity.MinValue, BidFee = Quantity.MinValue, AskRemainingQuantity = Quantity.MinValue, AskFee = Quantity.MinValue });
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MaxValue, TakerOrderId = OrderId.MaxValue, Timestamp = int.MaxValue, MatchQuantity = int.MaxValue, MatchRate = int.MaxValue, BidCost = Quantity.MaxValue, BidFee = Quantity.MaxValue, AskRemainingQuantity = Quantity.MaxValue, AskFee = Quantity.MaxValue });
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
            var bytes = new byte[126];
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("Fill Message must be of Size : 127", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
        {
            var bytes = new byte[128];
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("Fill Message must be of Size : 127", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[127];
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("Invalid Message", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[127];
            bytes[4] = (byte)MessageType.Fill;
            Exception ex = Assert.Throws<Exception>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal("version mismatch", ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MinValue, TakerOrderId = OrderId.MinValue, Timestamp = int.MinValue, MatchQuantity = int.MinValue, MatchRate = int.MinValue, BidCost = Quantity.MinValue, BidFee = Quantity.MinValue, AskRemainingQuantity = Quantity.MinValue, AskFee = Quantity.MinValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(127, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MinValue, fill.MakerOrderId);
            Assert.Equal(OrderId.MinValue, fill.TakerOrderId);
            Assert.Equal(int.MinValue, fill.MatchRate);
            Assert.Equal(int.MinValue, fill.MatchQuantity);
            Assert.Equal(int.MinValue, fill.Timestamp);
            Assert.Equal(Quantity.MinValue, fill.BidCost);
            Assert.Equal(Quantity.MinValue, fill.BidFee);
            Assert.Equal(Quantity.MinValue, fill.AskRemainingQuantity);
            Assert.Equal(Quantity.MinValue, fill.AskFee);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MaxValue, TakerOrderId = OrderId.MaxValue, Timestamp = int.MaxValue, MatchQuantity = int.MaxValue, MatchRate = int.MaxValue, BidCost = Quantity.MaxValue, BidFee = Quantity.MaxValue, AskRemainingQuantity = Quantity.MaxValue, AskFee = Quantity.MaxValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(127, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MaxValue, fill.MakerOrderId);
            Assert.Equal(OrderId.MaxValue, fill.TakerOrderId);
            Assert.Equal(int.MaxValue, fill.MatchRate);
            Assert.Equal(int.MaxValue, fill.MatchQuantity);
            Assert.Equal(int.MaxValue, fill.Timestamp);
            Assert.Equal(Quantity.MaxValue, fill.AskRemainingQuantity);
            Assert.Equal(Quantity.MaxValue, fill.BidCost);
            Assert.Equal(Quantity.MaxValue, fill.AskFee);
            Assert.Equal(Quantity.MaxValue, fill.BidFee);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = 12345678, TakerOrderId = 56789, Timestamp = 404, MatchQuantity = 2356, MatchRate = 9534, BidCost = 4347, BidFee = 76157, AskRemainingQuantity = 87135, AskFee = 12103 });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(127, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, fill.MakerOrderId);
            Assert.Equal((OrderId)56789, fill.TakerOrderId);
            Assert.Equal(9534, fill.MatchRate);
            Assert.Equal(2356, fill.MatchQuantity);
            Assert.Equal(404, fill.Timestamp);
            Assert.Equal(4347, fill.BidCost);
            Assert.Equal(76157, fill.BidFee);
            Assert.Equal(87135, fill.AskRemainingQuantity);
            Assert.Equal(12103, fill.AskFee);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_BidCostNull()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = 12345678, TakerOrderId = 56789, Timestamp = 404, MatchQuantity = 2356, MatchRate = 9534, BidCost = null, BidFee = null, AskRemainingQuantity = 87135, AskFee = 5434 });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(127, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, fill.MakerOrderId);
            Assert.Equal((OrderId)56789, fill.TakerOrderId);
            Assert.Equal(9534, fill.MatchRate);
            Assert.Equal(2356, fill.MatchQuantity);
            Assert.Equal(404, fill.Timestamp);
            Assert.Null(fill.BidCost);
            Assert.Null(fill.BidFee);
            Assert.Equal(87135, fill.AskRemainingQuantity);
            Assert.Equal(5434, fill.AskFee);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_AskRemainingQuantityNull()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = 12345678, TakerOrderId = 56789, Timestamp = 404, MatchQuantity = 2356, MatchRate = 9534, BidCost = 4347, BidFee = 891434, AskRemainingQuantity = null, AskFee = null });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(127, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, fill.MakerOrderId);
            Assert.Equal((OrderId)56789, fill.TakerOrderId);
            Assert.Equal(9534, fill.MatchRate);
            Assert.Equal(2356, fill.MatchQuantity);
            Assert.Equal(404, fill.Timestamp);
            Assert.Equal(4347, fill.BidCost);
            Assert.Equal(891434, fill.BidFee);
            Assert.Null(fill.AskRemainingQuantity);
            Assert.Null(fill.AskFee);
        }
    }
}
