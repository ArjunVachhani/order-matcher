﻿using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class OrderAcceptSerializerTest
    {
        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            Span<byte> bytes = stackalloc byte[OrderAcceptSerializer.MessageSize];
            OrderAcceptSerializer.Serialize(new OrderAccept { OrderId = OrderId.MinValue, Timestamp = int.MinValue, MessageSequence = long.MinValue }, bytes);
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            Span<byte> bytes = stackalloc byte[OrderAcceptSerializer.MessageSize];
            OrderAcceptSerializer.Serialize(new OrderAccept { OrderId = OrderId.MaxValue, Timestamp = int.MaxValue, MessageSequence = long.MaxValue }, bytes);
        }

        [Fact]
        public void Serialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => OrderAcceptSerializer.Serialize(null, null));
            Assert.Equal("orderAccept", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => OrderAcceptSerializer.Deserialize(null));
            Assert.Equal("bytes", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsLessThan35Bytes()
        {
            var bytes = new byte[22];
            Exception ex = Assert.Throws<Exception>(() => OrderAcceptSerializer.Deserialize(bytes));
            Assert.Equal("Order accept message must be of Size : 23", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
        {
            var bytes = new byte[24];
            Exception ex = Assert.Throws<Exception>(() => OrderAcceptSerializer.Deserialize(bytes));
            Assert.Equal("Order accept message must be of Size : 23", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[23];
            Exception ex = Assert.Throws<Exception>(() => OrderAcceptSerializer.Deserialize(bytes));
            Assert.Equal(Types.Constant.INVALID_MESSAGE, ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[23];
            bytes[4] = (byte)MessageType.OrderAccept;
            Exception ex = Assert.Throws<Exception>(() => OrderAcceptSerializer.Deserialize(bytes));
            Assert.Equal(Types.Constant.INVALID_VERSION, ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            Span<byte> bytes = stackalloc byte[OrderAcceptSerializer.MessageSize];
            OrderAcceptSerializer.Serialize(new OrderAccept { OrderId = OrderId.MinValue, Timestamp = int.MinValue, MessageSequence = long.MinValue }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(23, messageLength);
            var OrderAccept = OrderAcceptSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MinValue, OrderAccept.OrderId);
            Assert.Equal(long.MinValue, OrderAccept.MessageSequence);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            Span<byte> bytes = stackalloc byte[OrderAcceptSerializer.MessageSize];
            OrderAcceptSerializer.Serialize(new OrderAccept { OrderId = OrderId.MaxValue, Timestamp = int.MaxValue, MessageSequence = long.MaxValue }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(23, messageLength);
            var OrderAccept = OrderAcceptSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MaxValue, OrderAccept.OrderId);
            Assert.Equal(int.MaxValue, OrderAccept.Timestamp);
            Assert.Equal(long.MaxValue, OrderAccept.MessageSequence);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            Span<byte> bytes = stackalloc byte[OrderAcceptSerializer.MessageSize];
            OrderAcceptSerializer.Serialize(new OrderAccept { OrderId = 12345678, Timestamp = 404, MessageSequence = 456 }, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(23, messageLength);
            var OrderAccept = OrderAcceptSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, OrderAccept.OrderId);
            Assert.Equal(404, OrderAccept.Timestamp);
            Assert.Equal(456, OrderAccept.MessageSequence);
        }
    }
}
