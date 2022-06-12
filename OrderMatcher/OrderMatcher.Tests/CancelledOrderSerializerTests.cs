using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class CancelledOrderSerializerTests
    {
        private static readonly int messageSize = 84;

        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            Span<byte> bytes = stackalloc byte[CancelledOrderSerializer.MessageSize];
            CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = OrderId.MinValue, UserId = UserId.MinValue, Timestamp = int.MinValue, RemainingQuantity = int.MinValue, CancelReason = CancelReason.UserRequested, Cost = Quantity.MinValue, Fee = Quantity.MinValue, MessageSequence = long.MinValue }, bytes);
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            Span<byte> bytes = stackalloc byte[CancelledOrderSerializer.MessageSize];
            CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = OrderId.MaxValue, UserId = UserId.MaxValue, Timestamp = int.MaxValue, RemainingQuantity = int.MaxValue, CancelReason = CancelReason.ValidityExpired, Cost = Quantity.MaxValue, Fee = Quantity.MaxValue, MessageSequence = long.MaxValue }, bytes);
        }

        [Fact]
        public void Serialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => CancelledOrderSerializer.Serialize(null, null));
            Assert.Equal("cancelledOrder", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => CancelledOrderSerializer.Deserialize(null));
            Assert.Equal("bytes", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsLessThan35Bytes()
        {
            var bytes = new byte[messageSize - 1];
            Exception ex = Assert.Throws<Exception>(() => CancelledOrderSerializer.Deserialize(bytes));
            Assert.Equal($"Canceled Order Message must be of Size : {messageSize}", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
        {
            var bytes = new byte[messageSize + 1];
            Exception ex = Assert.Throws<Exception>(() => CancelledOrderSerializer.Deserialize(bytes));
            Assert.Equal($"Canceled Order Message must be of Size : {messageSize}", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[messageSize];
            Exception ex = Assert.Throws<Exception>(() => CancelledOrderSerializer.Deserialize(bytes));
            Assert.Equal(Types.Constant.INVALID_MESSAGE, ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[messageSize];
            bytes[4] = (byte)MessageType.Cancel;
            Exception ex = Assert.Throws<Exception>(() => CancelledOrderSerializer.Deserialize(bytes));
            Assert.Equal(Types.Constant.INVALID_VERSION, ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            Span<byte> bytes = stackalloc byte[CancelledOrderSerializer.MessageSize];
            CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = OrderId.MinValue, UserId = UserId.MinValue, Timestamp = int.MinValue, RemainingQuantity = int.MinValue, CancelReason = CancelReason.UserRequested, Cost = Quantity.MinValue, Fee = Quantity.MinValue, MessageSequence = long.MinValue }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(messageSize, messageLength);
            var cancelledOrder = CancelledOrderSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MinValue, cancelledOrder.OrderId);
            Assert.Equal(UserId.MinValue, cancelledOrder.UserId);
            Assert.Equal(int.MinValue, cancelledOrder.RemainingQuantity);
            Assert.Equal(int.MinValue, cancelledOrder.Timestamp);
            Assert.Equal(CancelReason.UserRequested, cancelledOrder.CancelReason);
            Assert.Equal(Quantity.MinValue, cancelledOrder.Cost);
            Assert.Equal(Quantity.MinValue, cancelledOrder.Fee);
            Assert.Equal(long.MinValue, cancelledOrder.MessageSequence);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            Span<byte> bytes = stackalloc byte[CancelledOrderSerializer.MessageSize];
            CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = OrderId.MaxValue, UserId = UserId.MaxValue, Timestamp = int.MaxValue, RemainingQuantity = int.MaxValue, CancelReason = CancelReason.ValidityExpired, Cost = Quantity.MaxValue, Fee = Quantity.MaxValue, MessageSequence = long.MaxValue }, bytes);

            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(messageSize, messageLength);
            var cancelledOrder = CancelledOrderSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MaxValue, cancelledOrder.OrderId);
            Assert.Equal(UserId.MaxValue, cancelledOrder.UserId);
            Assert.Equal(int.MaxValue, cancelledOrder.RemainingQuantity);
            Assert.Equal(int.MaxValue, cancelledOrder.Timestamp);
            Assert.Equal(CancelReason.ValidityExpired, cancelledOrder.CancelReason);
            Assert.Equal(Quantity.MaxValue, cancelledOrder.Cost);
            Assert.Equal(Quantity.MaxValue, cancelledOrder.Fee);
            Assert.Equal(long.MaxValue, cancelledOrder.MessageSequence);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            Span<byte> bytes = stackalloc byte[CancelledOrderSerializer.MessageSize];
            CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = 12345678, UserId = 8768, RemainingQuantity = 56789, Timestamp = 404, CancelReason = CancelReason.ValidityExpired, Cost = 12.13m, Fee = 92.005m, MessageSequence = 79242 }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(messageSize, messageLength);
            var cancelledOrder = CancelledOrderSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, cancelledOrder.OrderId);
            Assert.Equal((UserId)8768, cancelledOrder.UserId);
            Assert.Equal(56789, cancelledOrder.RemainingQuantity);
            Assert.Equal(404, cancelledOrder.Timestamp);
            Assert.Equal(CancelReason.ValidityExpired, cancelledOrder.CancelReason);
            Assert.Equal((Quantity)12.13m, cancelledOrder.Cost);
            Assert.Equal((Quantity)92.005m, cancelledOrder.Fee);
            Assert.Equal(79242, cancelledOrder.MessageSequence);
        }
    }
}
