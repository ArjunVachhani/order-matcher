using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class FillSerializerTests
    {
        private static readonly int messageSize = 151;

        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MinValue, TakerOrderId = OrderId.MinValue, MakerUserId = UserId.MinValue, TakerUserId = UserId.MinValue, Timestamp = int.MinValue, MatchQuantity = int.MinValue, MatchRate = int.MinValue, BidCost = Amount.MinValue, BidFee = Amount.MinValue, AskRemainingQuantity = Quantity.MinValue, AskFee = Amount.MinValue, MessageSequence = long.MinValue }, bytes);
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MaxValue, TakerOrderId = OrderId.MaxValue, MakerUserId = UserId.MaxValue, TakerUserId = UserId.MaxValue, Timestamp = int.MaxValue, MatchQuantity = int.MaxValue, MatchRate = int.MaxValue, BidCost = Amount.MaxValue, BidFee = Amount.MaxValue, AskRemainingQuantity = Quantity.MaxValue, AskFee = Amount.MaxValue, MessageSequence = long.MaxValue }, bytes);
        }

        [Fact]
        public void Serialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => FillSerializer.Serialize(null, null));
            Assert.Equal("fill", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsLessThan35Bytes()
        {
            var bytes = new byte[messageSize - 1];
            OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal($"Fill Message must be of Size : {messageSize}", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
        {
            var bytes = new byte[messageSize + 1];
            OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal($"Fill Message must be of Size : {messageSize}", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[messageSize];
            OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal(Types.Constant.INVALID_MESSAGE, ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[messageSize];
            bytes[4] = (byte)MessageType.Fill;
            OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => FillSerializer.Deserialize(bytes));
            Assert.Equal(Types.Constant.INVALID_VERSION, ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MinValue, TakerOrderId = OrderId.MinValue, MakerUserId = UserId.MinValue, TakerUserId = UserId.MinValue, Timestamp = int.MinValue, MatchQuantity = int.MinValue, MatchRate = int.MinValue, BidCost = Amount.MinValue, BidFee = Amount.MinValue, AskRemainingQuantity = Quantity.MinValue, AskFee = Amount.MinValue, MessageSequence = long.MinValue }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(messageSize, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MinValue, fill.MakerOrderId);
            Assert.Equal(OrderId.MinValue, fill.TakerOrderId);
            Assert.Equal(UserId.MinValue, fill.MakerUserId);
            Assert.Equal(UserId.MinValue, fill.TakerUserId);
            Assert.Equal(int.MinValue, fill.MatchRate);
            Assert.Equal(int.MinValue, fill.MatchQuantity);
            Assert.Equal(int.MinValue, fill.Timestamp);
            Assert.Equal(Amount.MinValue, fill.BidCost);
            Assert.Equal(Amount.MinValue, fill.BidFee);
            Assert.Equal(Quantity.MinValue, fill.AskRemainingQuantity);
            Assert.Equal(Amount.MinValue, fill.AskFee);
            Assert.Equal(long.MinValue, fill.MessageSequence);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(new Fill { MakerOrderId = OrderId.MaxValue, TakerOrderId = OrderId.MaxValue, MakerUserId = UserId.MaxValue, TakerUserId = UserId.MaxValue, Timestamp = int.MaxValue, MatchQuantity = int.MaxValue, MatchRate = int.MaxValue, BidCost = Amount.MaxValue, BidFee = Amount.MaxValue, AskRemainingQuantity = Quantity.MaxValue, AskFee = Amount.MaxValue, MessageSequence = long.MaxValue }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(messageSize, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MaxValue, fill.MakerOrderId);
            Assert.Equal(OrderId.MaxValue, fill.TakerOrderId);
            Assert.Equal(UserId.MaxValue, fill.MakerUserId);
            Assert.Equal(UserId.MaxValue, fill.TakerUserId);
            Assert.Equal(int.MaxValue, fill.MatchRate);
            Assert.Equal(int.MaxValue, fill.MatchQuantity);
            Assert.Equal(int.MaxValue, fill.Timestamp);
            Assert.Equal(Quantity.MaxValue, fill.AskRemainingQuantity);
            Assert.Equal(Amount.MaxValue, fill.BidCost);
            Assert.Equal(Amount.MaxValue, fill.AskFee);
            Assert.Equal(Amount.MaxValue, fill.BidFee);
            Assert.Equal(long.MaxValue, fill.MessageSequence);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(new Fill { MakerOrderId = 12345678, TakerOrderId = 56789, MakerUserId = 8728, TakerUserId = 530, Timestamp = 404, MatchQuantity = 2356, MatchRate = 9534, BidCost = 4347, BidFee = 76157, AskRemainingQuantity = 87135, AskFee = 12103, MessageSequence = 6812379 }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(messageSize, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, fill.MakerOrderId);
            Assert.Equal((OrderId)56789, fill.TakerOrderId);
            Assert.Equal((UserId)8728, fill.MakerUserId);
            Assert.Equal((UserId)530, fill.TakerUserId);
            Assert.Equal(9534, fill.MatchRate);
            Assert.Equal(2356, fill.MatchQuantity);
            Assert.Equal(404, fill.Timestamp);
            Assert.Equal(4347, fill.BidCost);
            Assert.Equal(76157, fill.BidFee);
            Assert.Equal(87135, fill.AskRemainingQuantity);
            Assert.Equal(12103, fill.AskFee);
            Assert.Equal(6812379, fill.MessageSequence);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_BidCostNull()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(new Fill { MakerOrderId = 12345678, TakerOrderId = 56789, MakerUserId = 8728, TakerUserId = 530, Timestamp = 404, MatchQuantity = 2356, MatchRate = 9534, BidCost = null, BidFee = null, AskRemainingQuantity = 87135, AskFee = 5434, MessageSequence = 123879 }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(messageSize, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, fill.MakerOrderId);
            Assert.Equal((OrderId)56789, fill.TakerOrderId);
            Assert.Equal((UserId)8728, fill.MakerUserId);
            Assert.Equal((UserId)530, fill.TakerUserId);
            Assert.Equal(9534, fill.MatchRate);
            Assert.Equal(2356, fill.MatchQuantity);
            Assert.Equal(404, fill.Timestamp);
            Assert.Null(fill.BidCost);
            Assert.Null(fill.BidFee);
            Assert.Equal(87135, fill.AskRemainingQuantity);
            Assert.Equal(5434, fill.AskFee);
            Assert.Equal(123879, fill.MessageSequence);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_AskRemainingQuantityNull()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(new Fill { MakerOrderId = 12345678, TakerOrderId = 56789, MakerUserId = 8728, TakerUserId = 530, Timestamp = 404, MatchQuantity = 2356, MatchRate = 9534, BidCost = 4347, BidFee = 891434, AskRemainingQuantity = null, AskFee = null, MessageSequence = 8089 }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(messageSize, messageLength);
            var fill = FillSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, fill.MakerOrderId);
            Assert.Equal((OrderId)56789, fill.TakerOrderId);
            Assert.Equal((UserId)8728, fill.MakerUserId);
            Assert.Equal((UserId)530, fill.TakerUserId);
            Assert.Equal(9534, fill.MatchRate);
            Assert.Equal(2356, fill.MatchQuantity);
            Assert.Equal(404, fill.Timestamp);
            Assert.Equal(4347, fill.BidCost);
            Assert.Equal(891434, fill.BidFee);
            Assert.Null(fill.AskRemainingQuantity);
            Assert.Null(fill.AskFee);
            Assert.Equal(8089, fill.MessageSequence);
        }
    }
}
