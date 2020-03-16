using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class CancelledOrderSerializerTests
    {
        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            var bytes = CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = OrderId.MinValue, Timestamp = int.MinValue, RemainingQuantity = int.MinValue, CancelReason = CancelReason.UserRequested, RemainingOrderAmount = decimal.MinValue });
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            var bytes = CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = OrderId.MaxValue, Timestamp = int.MaxValue, RemainingQuantity = int.MaxValue, CancelReason = CancelReason.ValidityExpired, RemainingOrderAmount = decimal.MaxValue });
        }

        [Fact]
        public void Serialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => CancelledOrderSerializer.Serialize(null));
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
            var bytes = new byte[51];
            Exception ex = Assert.Throws<Exception>(() => CancelledOrderSerializer.Deserialize(bytes));
            Assert.Equal("Canceled Order Message must be of Size : 52", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
        {
            var bytes = new byte[53];
            Exception ex = Assert.Throws<Exception>(() => CancelledOrderSerializer.Deserialize(bytes));
            Assert.Equal("Canceled Order Message must be of Size : 52", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[52];
            Exception ex = Assert.Throws<Exception>(() => CancelledOrderSerializer.Deserialize(bytes));
            Assert.Equal("Invalid Message", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[52];
            bytes[4] = (byte)MessageType.Cancel;
            Exception ex = Assert.Throws<Exception>(() => CancelledOrderSerializer.Deserialize(bytes));
            Assert.Equal("version mismatch", ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            var bytes = CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = OrderId.MinValue, Timestamp = int.MinValue, RemainingQuantity = int.MinValue, CancelReason = CancelReason.UserRequested, RemainingOrderAmount = decimal.MinValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(52, messageLength);
            var cancelledOrder = CancelledOrderSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MinValue, cancelledOrder.OrderId);
            Assert.Equal((Quantity)int.MinValue, cancelledOrder.RemainingQuantity);
            Assert.Equal(int.MinValue, cancelledOrder.Timestamp);
            Assert.Equal(CancelReason.UserRequested, cancelledOrder.CancelReason);
            Assert.Equal((Quantity)decimal.MinValue, cancelledOrder.RemainingOrderAmount);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            var bytes = CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = OrderId.MaxValue, Timestamp = int.MaxValue, RemainingQuantity = int.MaxValue, CancelReason = CancelReason.ValidityExpired, RemainingOrderAmount = decimal.MaxValue });

            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(52, messageLength);
            var cancelledOrder = CancelledOrderSerializer.Deserialize(bytes);
            Assert.Equal(OrderId.MaxValue, cancelledOrder.OrderId);
            Assert.Equal((Quantity)int.MaxValue, cancelledOrder.RemainingQuantity);
            Assert.Equal(int.MaxValue, cancelledOrder.Timestamp);
            Assert.Equal(CancelReason.ValidityExpired, cancelledOrder.CancelReason);
            Assert.Equal((Quantity)decimal.MaxValue, cancelledOrder.RemainingOrderAmount);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            var bytes = CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = 12345678, RemainingQuantity = 56789, Timestamp = 404, CancelReason = CancelReason.ValidityExpired, RemainingOrderAmount = 12.13m });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(52, messageLength);
            var cancelledOrder = CancelledOrderSerializer.Deserialize(bytes);
            Assert.Equal((OrderId)12345678, cancelledOrder.OrderId);
            Assert.Equal((Quantity)56789, cancelledOrder.RemainingQuantity);
            Assert.Equal(404, cancelledOrder.Timestamp);
            Assert.Equal(CancelReason.ValidityExpired, cancelledOrder.CancelReason);
            Assert.Equal((Quantity)12.13m, cancelledOrder.RemainingOrderAmount);
        }
    }
}
