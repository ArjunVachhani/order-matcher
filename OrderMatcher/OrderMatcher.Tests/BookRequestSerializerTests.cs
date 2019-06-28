using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class BookRequestSerializerTests
    {
        [Fact]
        public void Serialize_Doesnotthrowexception_Min()
        {
            var bytes = BookRequestSerializer.Serialize(new BookRequest { LevelCount = Int32.MinValue });
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_Max()
        {
            var bytes = BookRequestSerializer.Serialize(new BookRequest { LevelCount = Int32.MaxValue });
        }

        [Fact]
        public void Serialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => BookRequestSerializer.Serialize(null));
            Assert.Equal("bookRequest", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => BookRequestSerializer.Deserialize(null));
            Assert.Equal("bytes", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsLessThan35Bytes()
        {
            var bytes = new byte[6];
            Exception ex = Assert.Throws<Exception>(() => BookRequestSerializer.Deserialize(bytes));
            Assert.Equal("Book Request Message must be of Size : 7", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
        {
            var bytes = new byte[8];
            Exception ex = Assert.Throws<Exception>(() => BookRequestSerializer.Deserialize(bytes));
            Assert.Equal("Book Request Message must be of Size : 7", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[7];
            Exception ex = Assert.Throws<Exception>(() => BookRequestSerializer.Deserialize(bytes));
            Assert.Equal("Invalid Message", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[7];
            bytes[0] = (byte)MessageType.BookRequest;
            Exception ex = Assert.Throws<Exception>(() => BookRequestSerializer.Deserialize(bytes));
            Assert.Equal("version mismatch", ex.Message);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Min()
        {
            var bytes = BookRequestSerializer.Serialize(new BookRequest { LevelCount = int.MinValue });
            var cancelRequest = BookRequestSerializer.Deserialize(bytes);
            Assert.Equal(int.MinValue, cancelRequest.LevelCount);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception_Max()
        {
            var bytes = BookRequestSerializer.Serialize(new BookRequest { LevelCount = int.MaxValue });
            var cancelRequest = BookRequestSerializer.Deserialize(bytes);
            Assert.Equal(int.MaxValue, cancelRequest.LevelCount);
        }

        [Fact]
        public void Deserialize_Doesnotthrowexception()
        {
            var bytes = BookRequestSerializer.Serialize(new BookRequest { LevelCount = 12345678 });
            var cancelRequest = BookRequestSerializer.Deserialize(bytes);
            Assert.Equal(12345678, cancelRequest.LevelCount);
        }
    }
}
