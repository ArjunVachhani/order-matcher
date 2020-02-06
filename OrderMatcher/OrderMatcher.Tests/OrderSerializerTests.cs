using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class OrderSerializerTests
    {
        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            var bytes = OrderSerializer.Serialize(new Order { CancelOn = int.MinValue, IsBuy = false, OrderCondition = OrderCondition.None, OrderId = OrderId.MinValue, Price = int.MinValue, Quantity = int.MinValue, StopPrice = int.MinValue, TotalQuantity = int.MinValue, OrderAmount = decimal.MinValue });
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            var bytes = OrderSerializer.Serialize(new Order { CancelOn = int.MaxValue, IsBuy = true, OrderCondition = OrderCondition.FillOrKill, OrderId = OrderId.MaxValue, Price = int.MaxValue, Quantity = int.MaxValue, StopPrice = int.MaxValue, TotalQuantity = int.MaxValue, OrderAmount = decimal.MaxValue });
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
            var bytes = new byte[96];
            Exception ex = Assert.Throws<Exception>(() => OrderSerializer.Deserialize(bytes));
            Assert.Equal("Order Message must be of Size : 97", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan37Bytes()
        {
            var bytes = new byte[98];
            Exception ex = Assert.Throws<Exception>(() => OrderSerializer.Deserialize(bytes));
            Assert.Equal("Order Message must be of Size : 97", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[97];
            Exception ex = Assert.Throws<Exception>(() => OrderSerializer.Deserialize(bytes));
            Assert.Equal("Invalid Message", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[97];
            bytes[4] = (byte)MessageType.NewOrderRequest;
            Exception ex = Assert.Throws<Exception>(() => OrderSerializer.Deserialize(bytes));
            Assert.Equal("version mismatch", ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            var bytes = OrderSerializer.Serialize(new Order { CancelOn = int.MinValue, IsBuy = false, OrderCondition = OrderCondition.None, OrderId = OrderId.MinValue, Price = int.MinValue, Quantity = int.MinValue, StopPrice = int.MinValue, TotalQuantity = int.MinValue, OrderAmount = decimal.MinValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(97, messageLength);
            var order = OrderSerializer.Deserialize(bytes);
            Assert.Equal(int.MinValue, order.CancelOn);
            Assert.False(order.IsBuy);
            Assert.Equal(OrderCondition.None, order.OrderCondition);
            Assert.Equal(OrderId.MinValue, order.OrderId);
            Assert.Equal((Price)int.MinValue, order.Price);
            Assert.Equal((Quantity)int.MinValue, order.Quantity);
            Assert.Equal((Price)int.MinValue, order.StopPrice);
            Assert.Equal((Quantity)int.MinValue, order.TotalQuantity);
            Assert.Equal((Quantity)decimal.MinValue, order.OrderAmount);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            var bytes = OrderSerializer.Serialize(new Order { CancelOn = int.MaxValue, IsBuy = true, OrderCondition = OrderCondition.FillOrKill, OrderId = OrderId.MaxValue, Price = int.MaxValue, Quantity = int.MaxValue, StopPrice = int.MaxValue, TotalQuantity = int.MaxValue, OrderAmount = decimal.MaxValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(97, messageLength);
            var order = OrderSerializer.Deserialize(bytes);
            Assert.Equal(int.MaxValue, order.CancelOn);
            Assert.True(order.IsBuy);
            Assert.Equal(OrderCondition.FillOrKill, order.OrderCondition);
            Assert.Equal(OrderId.MaxValue, order.OrderId);
            Assert.Equal((Price)int.MaxValue, order.Price);
            Assert.Equal((Quantity)int.MaxValue, order.Quantity);
            Assert.Equal((Price)int.MaxValue, order.StopPrice);
            Assert.Equal((Quantity)int.MaxValue, order.TotalQuantity);
            Assert.Equal((Quantity)decimal.MaxValue, order.OrderAmount);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            var bytes = OrderSerializer.Serialize(new Order { CancelOn = 12345678, IsBuy = true, OrderCondition = OrderCondition.ImmediateOrCancel, OrderId = 56789, Price = 404, Quantity = 2356, StopPrice = 9534, TotalQuantity = 7878234, OrderAmount = 12345.6789m });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(97, messageLength);
            var order = OrderSerializer.Deserialize(bytes);
            Assert.Equal(12345678, order.CancelOn);
            Assert.True(order.IsBuy);
            Assert.Equal(OrderCondition.ImmediateOrCancel, order.OrderCondition);
            Assert.Equal((OrderId)56789, order.OrderId);
            Assert.Equal((Price)404, order.Price);
            Assert.Equal((Quantity)2356, order.Quantity);
            Assert.Equal((Price)9534, order.StopPrice);
            Assert.Equal((Quantity)7878234, order.TotalQuantity);
            Assert.Equal((Quantity)12345.6789m, order.OrderAmount);            
        }
    }
}
