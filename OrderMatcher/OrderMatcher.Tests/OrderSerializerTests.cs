using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class OrderSerializerTests
    {
        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            var order1 = new Order { CancelOn = int.MinValue, IsBuy = false, OrderId = OrderId.MinValue, Price = int.MinValue, FeeId = short.MinValue };
            var orderWrapper = new OrderWrapper() { StopPrice = int.MinValue, TotalQuantity = int.MinValue, TipQuantity = int.MinValue, OrderCondition = OrderCondition.None, OrderAmount = Quantity.MinValue, Order = order1 };
            var bytes = OrderSerializer.Serialize(orderWrapper);
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            var order1 = new Order { CancelOn = int.MaxValue, IsBuy = true, OrderId = OrderId.MaxValue, Price = int.MaxValue, FeeId = short.MaxValue };
            var orderWrapper = new OrderWrapper() { StopPrice = int.MaxValue, TotalQuantity = int.MaxValue, TipQuantity = int.MaxValue, OrderCondition = OrderCondition.FillOrKill, OrderAmount = Quantity.MaxValue, Order = order1 };
            var bytes = OrderSerializer.Serialize(orderWrapper);
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
            var bytes = new byte[98];
            Exception ex = Assert.Throws<Exception>(() => OrderSerializer.Deserialize(bytes));
            Assert.Equal("Order Message must be of Size : 99", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan37Bytes()
        {
            var bytes = new byte[100];
            Exception ex = Assert.Throws<Exception>(() => OrderSerializer.Deserialize(bytes));
            Assert.Equal("Order Message must be of Size : 99", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[99];
            Exception ex = Assert.Throws<Exception>(() => OrderSerializer.Deserialize(bytes));
            Assert.Equal("Invalid Message", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[99];
            bytes[4] = (byte)MessageType.NewOrderRequest;
            Exception ex = Assert.Throws<Exception>(() => OrderSerializer.Deserialize(bytes));
            Assert.Equal("version mismatch", ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            var order1 = new Order { CancelOn = int.MinValue, IsBuy = false, OrderId = OrderId.MinValue, Price = int.MinValue, FeeId = short.MinValue };
            var orderWrapper = new OrderWrapper() { StopPrice = int.MinValue, TotalQuantity = int.MinValue, TipQuantity = int.MinValue, OrderCondition = OrderCondition.None, OrderAmount = Quantity.MinValue, Order = order1 };
            var bytes = OrderSerializer.Serialize(orderWrapper);
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(99, messageLength);
            var order = OrderSerializer.Deserialize(bytes);
            Assert.Equal(int.MinValue, order.Order.CancelOn);
            Assert.False(order.Order.IsBuy);
            Assert.Equal(OrderCondition.None, order.OrderCondition);
            Assert.Equal(OrderId.MinValue, order.Order.OrderId);
            Assert.Equal(int.MinValue, order.Order.Price);
            Assert.Equal(0, order.TipQuantity);
            Assert.False(order.Order.IsStop);
            Assert.Equal(int.MinValue, order.StopPrice);
            Assert.Equal(int.MinValue, order.TotalQuantity);
            Assert.Equal(Quantity.MinValue, order.OrderAmount);
            Assert.Equal(short.MinValue, order.Order.FeeId);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            var order1 = new Order { CancelOn = int.MaxValue, IsBuy = true, OrderId = OrderId.MaxValue, Price = int.MaxValue, FeeId = short.MaxValue };
            var orderWrapper = new OrderWrapper() { StopPrice = int.MaxValue, TotalQuantity = int.MaxValue, TipQuantity = int.MaxValue, OrderCondition = OrderCondition.FillOrKill, OrderAmount = Quantity.MaxValue, Order = order1 };
            var bytes = OrderSerializer.Serialize(orderWrapper);
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(99, messageLength);
            var order = OrderSerializer.Deserialize(bytes);
            Assert.Equal(int.MaxValue, order.Order.CancelOn);
            Assert.True(order.Order.IsBuy);
            Assert.Equal(OrderCondition.FillOrKill, order.OrderCondition);
            Assert.Equal(OrderId.MaxValue, order.Order.OrderId);
            Assert.Equal(int.MaxValue, order.Order.Price);
            Assert.Equal(int.MaxValue, order.TipQuantity);
            Assert.True(order.Order.IsStop);
            Assert.Equal(int.MaxValue, order.StopPrice);
            Assert.Equal(int.MaxValue, order.TotalQuantity);
            Assert.Equal(Quantity.MaxValue, order.OrderAmount);
            Assert.Equal(short.MaxValue, order.Order.FeeId);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            var order1 = new Order { CancelOn = 12345678, IsBuy = true, OrderId = 56789, Price = 404, FeeId = 69 };
            var orderWrapper = new OrderWrapper() { StopPrice = 9534, TotalQuantity = 7878234, TipQuantity = 2356, OrderCondition = OrderCondition.ImmediateOrCancel, OrderAmount = 12345.6789m, Order = order1 };
            var bytes = OrderSerializer.Serialize(orderWrapper);
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(99, messageLength);
            var order = OrderSerializer.Deserialize(bytes);
            Assert.Equal(12345678, order.Order.CancelOn);
            Assert.True(order.Order.IsBuy);
            Assert.Equal(OrderCondition.ImmediateOrCancel, order.OrderCondition);
            Assert.Equal((OrderId)56789, order.Order.OrderId);
            Assert.Equal(404, order.Order.Price);
            Assert.Equal(2356, order.TipQuantity);
            Assert.True(order.Order.IsStop);
            Assert.Equal(9534, order.StopPrice);
            Assert.Equal(7878234, order.TotalQuantity);
            Assert.Equal((Quantity)12345.6789m, order.OrderAmount);
            Assert.Equal(69, order.Order.FeeId);
        }
    }
}
