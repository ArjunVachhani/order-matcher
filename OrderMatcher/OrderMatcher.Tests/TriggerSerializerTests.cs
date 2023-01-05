using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class TriggerSerializerTests
    {
        private static readonly int messageSize = 35;

        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            Span<byte> bytes = stackalloc byte[OrderTriggerSerializer.MessageSize];
            OrderTriggerSerializer.Serialize(new OrderTrigger { OrderId = OrderId.MinValue, UserId = UserId.MinValue, Timestamp = int.MinValue, MessageSequence = long.MinValue }, bytes);
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            Span<byte> bytes = stackalloc byte[OrderTriggerSerializer.MessageSize];
            OrderTriggerSerializer.Serialize(new OrderTrigger { OrderId = OrderId.MaxValue, UserId = UserId.MaxValue, Timestamp = int.MaxValue, MessageSequence = long.MaxValue }, bytes);
        }

        [Fact]
        public void Serialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => OrderTriggerSerializer.Serialize(null, null));
            Assert.Equal("orderTrigger", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => OrderTriggerSerializer.Deserialize(null));
            Assert.Equal("bytes", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsLessThan35Bytes()
        {
            var bytes = new byte[messageSize - 1];
            Exception ex = Assert.Throws<Exception>(() => OrderTriggerSerializer.Deserialize(bytes));
            Assert.Equal($"Order Trigger Message must be of Size : {messageSize}", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
        {
            var bytes = new byte[messageSize + 1];
            Exception ex = Assert.Throws<Exception>(() => OrderTriggerSerializer.Deserialize(bytes));
            Assert.Equal($"Order Trigger Message must be of Size : {messageSize}", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[messageSize];
            Exception ex = Assert.Throws<Exception>(() => OrderTriggerSerializer.Deserialize(bytes));
            Assert.Equal(Types.Constant.INVALID_MESSAGE, ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[messageSize];
            bytes[4] = (byte)MessageType.OrderTrigger;
            Exception ex = Assert.Throws<Exception>(() => OrderTriggerSerializer.Deserialize(bytes));
            Assert.Equal(Types.Constant.INVALID_VERSION, ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            Span<byte> bytes = stackalloc byte[OrderTriggerSerializer.MessageSize];
            OrderTriggerSerializer.Serialize(new OrderTrigger { OrderId = OrderId.MinValue, UserId = UserId.MinValue, Timestamp = int.MinValue, MessageSequence = long.MinValue }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(messageSize, messageLength);
            var orderTrigger = OrderTriggerSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MinValue, orderTrigger.OrderId);
            Assert.Equal(UserId.MinValue, orderTrigger.UserId);
            Assert.Equal(int.MinValue, orderTrigger.Timestamp);
            Assert.Equal(long.MinValue, orderTrigger.MessageSequence);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            Span<byte> bytes = stackalloc byte[OrderTriggerSerializer.MessageSize];
            OrderTriggerSerializer.Serialize(new OrderTrigger { OrderId = OrderId.MaxValue, UserId = UserId.MaxValue, Timestamp = int.MaxValue, MessageSequence = long.MaxValue }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(messageSize, messageLength);
            var orderTrigger = OrderTriggerSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MaxValue, orderTrigger.OrderId);
            Assert.Equal(UserId.MaxValue, orderTrigger.UserId);
            Assert.Equal(int.MaxValue, orderTrigger.Timestamp);
            Assert.Equal(long.MaxValue, orderTrigger.MessageSequence);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            Span<byte> bytes = stackalloc byte[OrderTriggerSerializer.MessageSize];
            OrderTriggerSerializer.Serialize(new OrderTrigger { OrderId = 12345678, UserId = 139, Timestamp = 404, MessageSequence = 972 }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(messageSize, messageLength);
            var orderTrigger = OrderTriggerSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, orderTrigger.OrderId);
            Assert.Equal((UserId)139, orderTrigger.UserId);
            Assert.Equal(404, orderTrigger.Timestamp);
            Assert.Equal(972, orderTrigger.MessageSequence);
        }
    }
}
