using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class OrderSerializerTests
    {
        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            var bytes = OrderSerializer.Serialize(new Order { CancelOn = long.MinValue, IsBuy = false, OrderCondition = OrderCondition.None, OrderId = ulong.MinValue, Price = int.MinValue, Quantity = int.MinValue, StopPrice = int.MinValue, TotalQuantity = int.MinValue });
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            var bytes = OrderSerializer.Serialize(new Order { CancelOn = long.MaxValue, IsBuy = true, OrderCondition = OrderCondition.FillOrKill, OrderId = ulong.MaxValue, Price = int.MaxValue, Quantity = int.MaxValue, StopPrice = int.MaxValue, TotalQuantity = int.MaxValue });
        }

        [Fact]
        public void Serialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => OrderSerializer.Serialize(null));
            Assert.Equal("order", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => OrderSerializer.Deserialize(null));
            Assert.Equal("bytes", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsLessThan37Bytes()
        {
            var bytes = new byte[40];
            Exception ex = Assert.Throws<Exception>(() => OrderSerializer.Deserialize(bytes));
            Assert.Equal("Order Message must be of Size : 41", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan37Bytes()
        {
            var bytes = new byte[42];
            Exception ex = Assert.Throws<Exception>(() => OrderSerializer.Deserialize(bytes));
            Assert.Equal("Order Message must be of Size : 41", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[41];
            Exception ex = Assert.Throws<Exception>(() => OrderSerializer.Deserialize(bytes));
            Assert.Equal("Invalid Message", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[41];
            bytes[4] = (byte)MessageType.NewOrderRequest;
            Exception ex = Assert.Throws<Exception>(() => OrderSerializer.Deserialize(bytes));
            Assert.Equal("version mismatch", ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            var bytes = OrderSerializer.Serialize(new Order { CancelOn = long.MinValue, IsBuy = false, OrderCondition = OrderCondition.None, OrderId = ulong.MinValue, Price = int.MinValue, Quantity = int.MinValue, StopPrice = int.MinValue, TotalQuantity = int.MinValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(41, messageLength);
            var order = OrderSerializer.Deserialize(bytes);
            Assert.Equal(long.MinValue, order.CancelOn);
            Assert.False(order.IsBuy);
            Assert.Equal(OrderCondition.None, order.OrderCondition);
            Assert.Equal(ulong.MinValue, order.OrderId);
            Assert.Equal((Price)int.MinValue, order.Price);
            Assert.Equal((Quantity)int.MinValue, order.Quantity);
            Assert.Equal((Price)int.MinValue, order.StopPrice);
            Assert.Equal((Quantity)int.MinValue, order.TotalQuantity);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            var bytes = OrderSerializer.Serialize(new Order { CancelOn = long.MaxValue, IsBuy = true, OrderCondition = OrderCondition.FillOrKill, OrderId = ulong.MaxValue, Price = int.MaxValue, Quantity = int.MaxValue, StopPrice = int.MaxValue, TotalQuantity = int.MaxValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(41, messageLength);
            var order = OrderSerializer.Deserialize(bytes);
            Assert.Equal(long.MaxValue, order.CancelOn);
            Assert.True(order.IsBuy);
            Assert.Equal(OrderCondition.FillOrKill, order.OrderCondition);
            Assert.Equal(ulong.MaxValue, order.OrderId);
            Assert.Equal((Price)int.MaxValue, order.Price);
            Assert.Equal((Quantity)int.MaxValue, order.Quantity);
            Assert.Equal((Price)int.MaxValue, order.StopPrice);
            Assert.Equal((Quantity)int.MaxValue, order.TotalQuantity);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            var bytes = OrderSerializer.Serialize(new Order { CancelOn = 12345678, IsBuy = true, OrderCondition = OrderCondition.ImmediateOrCancel, OrderId = 56789, Price = 404, Quantity = 2356, StopPrice = 9534, TotalQuantity = 7878234 });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(41, messageLength);
            var order = OrderSerializer.Deserialize(bytes);
            Assert.Equal(12345678, order.CancelOn);
            Assert.True(order.IsBuy);
            Assert.Equal(OrderCondition.ImmediateOrCancel, order.OrderCondition);
            Assert.Equal((ulong)56789, order.OrderId);
            Assert.Equal((Price)404, order.Price);
            Assert.Equal((Quantity)2356, order.Quantity);
            Assert.Equal((Price)9534, order.StopPrice);
            Assert.Equal((Quantity)7878234, order.TotalQuantity);
        }
    }
}
