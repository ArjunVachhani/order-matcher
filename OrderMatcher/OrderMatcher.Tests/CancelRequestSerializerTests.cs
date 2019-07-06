using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class CancelRequestSerializerTests
    {
        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            var bytes = CancelRequestSerializer.Serialize(new CancelRequest { OrderId = ulong.MinValue });
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            var bytes = CancelRequestSerializer.Serialize(new CancelRequest { OrderId = ulong.MaxValue });
        }

        [Fact]
        public void Serialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => CancelRequestSerializer.Serialize(null));
            Assert.Equal("cancelRequest", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => CancelRequestSerializer.Deserialize(null));
            Assert.Equal("bytes", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsLessThan35Bytes()
        {
            var bytes = new byte[14];
            Exception ex = Assert.Throws<Exception>(() => CancelRequestSerializer.Deserialize(bytes));
            Assert.Equal("Cancel Request Message must be of Size : 15", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
        {
            var bytes = new byte[16];
            Exception ex = Assert.Throws<Exception>(() => CancelRequestSerializer.Deserialize(bytes));
            Assert.Equal("Cancel Request Message must be of Size : 15", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[15];
            Exception ex = Assert.Throws<Exception>(() => CancelRequestSerializer.Deserialize(bytes));
            Assert.Equal("Invalid Message", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[15];
            bytes[4] = (byte)MessageType.CancelRequest;
            Exception ex = Assert.Throws<Exception>(() => CancelRequestSerializer.Deserialize(bytes));
            Assert.Equal("version mismatch", ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            var bytes = CancelRequestSerializer.Serialize(new CancelRequest { OrderId = ulong.MinValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(15, messageLength);
            var cancelRequest = CancelRequestSerializer.Deserialize(bytes);
            Assert.Equal(ulong.MinValue, cancelRequest.OrderId);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            var bytes = CancelRequestSerializer.Serialize(new CancelRequest { OrderId = ulong.MaxValue });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(15, messageLength);
            var cancelRequest = CancelRequestSerializer.Deserialize(bytes);
            Assert.Equal(ulong.MaxValue, cancelRequest.OrderId);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            var bytes = CancelRequestSerializer.Serialize(new CancelRequest { OrderId = 12345678 });
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(15, messageLength);
            var cancelRequest = CancelRequestSerializer.Deserialize(bytes);
            Assert.Equal((ulong)12345678, cancelRequest.OrderId);
        }
    }
}
