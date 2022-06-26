using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class OrderSerializerTests
    {
        private static readonly int messageSize = 131;

        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            Span<byte> bytes = stackalloc byte[OrderSerializer.MessageSize];
            var orderWrapper = new Order()
            {
                StopPrice = Price.MinValue,
                TotalQuantity = Quantity.MinValue,
                TipQuantity = Quantity.MinValue,
                OrderCondition = OrderCondition.None,
                OrderAmount = Quantity.MinValue,
                CancelOn = int.MinValue,
                IsBuy = false,
                OrderId = OrderId.MinValue,
                Price = Price.MinValue,
                FeeId = short.MinValue,
                Cost = Quantity.MinValue,
                Fee = Quantity.MinValue,
                OpenQuantity = Quantity.MinValue
            };
            OrderSerializer.Serialize(orderWrapper, bytes);
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            Span<byte> bytes = stackalloc byte[OrderSerializer.MessageSize];
            var orderWrapper = new Order()
            {
                StopPrice = Price.MaxValue,
                TotalQuantity = Quantity.MaxValue,
                TipQuantity = Quantity.MaxValue,
                OrderCondition = OrderCondition.FillOrKill,
                OrderAmount = Quantity.MaxValue,
                CancelOn = int.MaxValue,
                IsBuy = true,
                OrderId = OrderId.MaxValue,
                Price = Price.MaxValue,
                FeeId = short.MaxValue,
                Cost = Quantity.MaxValue,
                Fee = Quantity.MaxValue,
                OpenQuantity = Quantity.MaxValue
            };
            OrderSerializer.Serialize(orderWrapper, bytes);
        }

        [Fact]
        public void Serialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => OrderSerializer.Serialize(null, null));
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
            var bytes = new byte[messageSize - 1];
            Exception ex = Assert.Throws<Exception>(() => OrderSerializer.Deserialize(bytes));
            Assert.Equal($"Order Message must be of Size : {messageSize}", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan37Bytes()
        {
            var bytes = new byte[messageSize + 1];
            Exception ex = Assert.Throws<Exception>(() => OrderSerializer.Deserialize(bytes));
            Assert.Equal($"Order Message must be of Size : {messageSize}", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[messageSize];
            Exception ex = Assert.Throws<Exception>(() => OrderSerializer.Deserialize(bytes));
            Assert.Equal(Types.Constant.INVALID_MESSAGE, ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[messageSize];
            bytes[4] = (byte)MessageType.NewOrderRequest;
            Exception ex = Assert.Throws<Exception>(() => OrderSerializer.Deserialize(bytes));
            Assert.Equal(Types.Constant.INVALID_VERSION, ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            Span<byte> bytes = stackalloc byte[OrderSerializer.MessageSize];
            var inputOrder = new Order()
            {
                StopPrice = Price.MinValue,
                TotalQuantity = Quantity.MinValue,
                TipQuantity = Quantity.MinValue,
                OrderCondition = OrderCondition.None,
                OrderAmount = Quantity.MinValue,
                CancelOn = int.MinValue,
                IsBuy = false,
                OrderId = OrderId.MinValue,
                Price = Price.MinValue,
                FeeId = short.MinValue,
                Cost = Quantity.MinValue,
                Fee = Quantity.MinValue,
                OpenQuantity = Quantity.MinValue
            };
            OrderSerializer.Serialize(inputOrder, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(messageSize, messageLength);
            var order = OrderSerializer.Deserialize(bytes);
            Assert.Equal(int.MinValue, order.CancelOn);
            Assert.False(order.IsBuy);
            Assert.Equal(OrderCondition.None, order.OrderCondition);
            Assert.Equal(OrderId.MinValue, order.OrderId);
            Assert.Equal(Price.MinValue, order.Price);
            Assert.Equal(0, order.TipQuantity);
            Assert.False(order.IsStop);
            Assert.Equal(Price.MinValue, order.StopPrice);
            Assert.Equal(Quantity.MinValue, order.TotalQuantity);
            Assert.Equal(Quantity.MinValue, order.OrderAmount);
            Assert.Equal(short.MinValue, order.FeeId);
            Assert.Equal(Quantity.MinValue, order.Cost);
            Assert.Equal(Quantity.MinValue, order.Fee);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            Span<byte> bytes = stackalloc byte[OrderSerializer.MessageSize];
            var orderWrapper = new Order()
            {
                StopPrice = Price.MaxValue,
                TotalQuantity = Quantity.MaxValue,
                TipQuantity = Quantity.MaxValue,
                OrderCondition = OrderCondition.FillOrKill,
                OrderAmount = Quantity.MaxValue,
                CancelOn = int.MaxValue,
                IsBuy = true,
                OrderId = OrderId.MaxValue,
                Price = Price.MaxValue,
                FeeId = short.MaxValue,
                Cost = Quantity.MaxValue,
                Fee = Quantity.MaxValue,
                OpenQuantity = Quantity.MaxValue
            };
            OrderSerializer.Serialize(orderWrapper, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(messageSize, messageLength);
            var order = OrderSerializer.Deserialize(bytes);
            Assert.Equal(int.MaxValue, order.CancelOn);
            Assert.True(order.IsBuy);
            Assert.Equal(OrderCondition.FillOrKill, order.OrderCondition);
            Assert.Equal(OrderId.MaxValue, order.OrderId);
            Assert.Equal(Price.MaxValue, order.Price);
            Assert.Equal(Quantity.MaxValue, order.TipQuantity);
            Assert.True(order.IsStop);
            Assert.Equal(Price.MaxValue, order.StopPrice);
            Assert.Equal(Quantity.MaxValue, order.TotalQuantity);
            Assert.Equal(Quantity.MaxValue, order.OrderAmount);
            Assert.Equal(short.MaxValue, order.FeeId);
            Assert.Equal(Quantity.MaxValue, order.Cost);
            Assert.Equal(Quantity.MaxValue, order.Fee);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            Span<byte> bytes = stackalloc byte[OrderSerializer.MessageSize];
            var orderWrapper = new Order() { StopPrice = 9534, TotalQuantity = 7878234, TipQuantity = 2356, OrderCondition = OrderCondition.ImmediateOrCancel, OrderAmount = 12345.6789m, CancelOn = 12345678, IsBuy = true, OrderId = 56789, Price = 404, FeeId = 69, Cost = 253.15m, Fee = 8649.123m, OpenQuantity = 546 };
            OrderSerializer.Serialize(orderWrapper, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(messageSize, messageLength);
            var order = OrderSerializer.Deserialize(bytes);
            Assert.Equal(12345678, order.CancelOn);
            Assert.True(order.IsBuy);
            Assert.Equal(OrderCondition.ImmediateOrCancel, order.OrderCondition);
            Assert.Equal((OrderId)56789, order.OrderId);
            Assert.Equal(404, order.Price);
            Assert.Equal(2356, order.TipQuantity);
            Assert.True(order.IsStop);
            Assert.Equal(9534, order.StopPrice);
            Assert.Equal(7878234, order.TotalQuantity);
            Assert.Equal((Quantity)12345.6789m, order.OrderAmount);
            Assert.Equal(69, order.FeeId);
            Assert.Equal((Quantity)253.15m, order.Cost);
            Assert.Equal((Quantity)8649.123m, order.Fee);
            Assert.Equal((Quantity)2356, order.OpenQuantity);
        }
    }
}
