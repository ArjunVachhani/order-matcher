using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class CancelledOrderSerializerTests
    {
        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            var bytes = CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = ulong.MinValue, Timestamp = long.MinValue, RemainingQuantity = int.MinValue, CancelReason = CancelReason.UserRequested });
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            var bytes = CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = ulong.MaxValue, Timestamp = long.MaxValue, RemainingQuantity = int.MaxValue, CancelReason = CancelReason.ValidityExpired });
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
            var bytes = new byte[23];
            Exception ex = Assert.Throws<Exception>(() => CancelledOrderSerializer.Deserialize(bytes));
            Assert.Equal("Canceled Order Message must be of Size : 24", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
        {
            var bytes = new byte[25];
            Exception ex = Assert.Throws<Exception>(() => CancelledOrderSerializer.Deserialize(bytes));
            Assert.Equal("Canceled Order Message must be of Size : 24", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[24];
            Exception ex = Assert.Throws<Exception>(() => CancelledOrderSerializer.Deserialize(bytes));
            Assert.Equal("Invalid Message", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[24];
            bytes[0] = (byte)MessageType.Cancel;
            Exception ex = Assert.Throws<Exception>(() => CancelledOrderSerializer.Deserialize(bytes));
            Assert.Equal("version mismatch", ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            var bytes = CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = ulong.MinValue, Timestamp = long.MinValue, RemainingQuantity = int.MinValue, CancelReason = CancelReason.UserRequested });
            var cancelledOrder = CancelledOrderSerializer.Deserialize(bytes);
            Assert.Equal(ulong.MinValue, cancelledOrder.OrderId);
            Assert.Equal((Quantity)int.MinValue, cancelledOrder.RemainingQuantity);
            Assert.Equal(long.MinValue, cancelledOrder.Timestamp);
            Assert.Equal(CancelReason.UserRequested, cancelledOrder.CancelReason);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            var bytes = CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = ulong.MaxValue, Timestamp = long.MaxValue, RemainingQuantity = int.MaxValue, CancelReason = CancelReason.ValidityExpired });
            var cancelledOrder = CancelledOrderSerializer.Deserialize(bytes);
            Assert.Equal(ulong.MaxValue, cancelledOrder.OrderId);
            Assert.Equal((Quantity)int.MaxValue, cancelledOrder.RemainingQuantity);
            Assert.Equal(long.MaxValue, cancelledOrder.Timestamp);
            Assert.Equal(CancelReason.ValidityExpired, cancelledOrder.CancelReason);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            var bytes = CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = 12345678, RemainingQuantity = 56789, Timestamp = 404, CancelReason = CancelReason.ValidityExpired });
            var cancelledOrder = CancelledOrderSerializer.Deserialize(bytes);
            Assert.Equal((ulong)12345678, cancelledOrder.OrderId);
            Assert.Equal((Quantity)56789, cancelledOrder.RemainingQuantity);
            Assert.Equal(404, cancelledOrder.Timestamp);
            Assert.Equal(CancelReason.ValidityExpired, cancelledOrder.CancelReason);
        }
    }
}
