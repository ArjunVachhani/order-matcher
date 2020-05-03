using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class OrderAcceptSerializerTest
    {
        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            var bytes = OrderAcceptSerializer.Serialize(new OrderAccept { OrderId = OrderId.MinValue, Timestamp = int.MinValue });
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            var bytes = OrderAcceptSerializer.Serialize(new OrderAccept { OrderId = OrderId.MaxValue, Timestamp = int.MaxValue });
        }

        [Fact]
        public void Serialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => OrderAcceptSerializer.Serialize(null));
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
            var bytes = new byte[14];
            Exception ex = Assert.Throws<Exception>(() => OrderAcceptSerializer.Deserialize(bytes));
            Assert.Equal("Order accept message must be of Size : 15", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
        {
            var bytes = new byte[16];
            Exception ex = Assert.Throws<Exception>(() => OrderAcceptSerializer.Deserialize(bytes));
            Assert.Equal("Order accept message must be of Size : 15", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[15];
            Exception ex = Assert.Throws<Exception>(() => OrderAcceptSerializer.Deserialize(bytes));
            Assert.Equal("Invalid Message", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[15];
            bytes[4] = (byte)MessageType.OrderAccept;
            Exception ex = Assert.Throws<Exception>(() => OrderAcceptSerializer.Deserialize(bytes));
            Assert.Equal("version mismatch", ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            var bytes = OrderAcceptSerializer.Serialize(new OrderAccept { OrderId = OrderId.MinValue, Timestamp = int.MinValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(15, messageLength);
            var OrderAccept = OrderAcceptSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MinValue, OrderAccept.OrderId);
            Assert.Equal(int.MinValue, OrderAccept.Timestamp);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            var bytes = OrderAcceptSerializer.Serialize(new OrderAccept { OrderId = OrderId.MaxValue, Timestamp = int.MaxValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(15, messageLength);
            var OrderAccept = OrderAcceptSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MaxValue, OrderAccept.OrderId);
            Assert.Equal(int.MaxValue, OrderAccept.Timestamp);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            var bytes = OrderAcceptSerializer.Serialize(new OrderAccept { OrderId = 12345678, Timestamp = 404 });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(15, messageLength);
            var OrderAccept = OrderAcceptSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, OrderAccept.OrderId);
            Assert.Equal(404, OrderAccept.Timestamp);
        }
    }
}
