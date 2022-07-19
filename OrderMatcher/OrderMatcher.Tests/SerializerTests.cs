using OrderMatcher.Types.Serializers;
using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class SerializerTests
    {
        [Fact]
        public void WriteShortTest()
        {
            var arr1 = new byte[2];
            Serializer.Write(arr1, (short)5);
            var arr2 = BitConverter.GetBytes((short)5);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Fact]
        public void WriteShortMinTest()
        {
            var arr1 = new byte[2];
            Serializer.Write(arr1, short.MinValue);
            var arr2 = BitConverter.GetBytes(short.MinValue);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Fact]
        public void WriteShortMaxTest()
        {
            var arr1 = new byte[2];
            Serializer.Write(arr1, short.MaxValue);
            var arr2 = BitConverter.GetBytes(short.MaxValue);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Fact]
        public void WriteIntTest()
        {
            var arr1 = new byte[4];
            Serializer.Write(arr1, 5);
            var arr2 = BitConverter.GetBytes(5);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Fact]
        public void WriteIntMinTest()
        {
            var arr1 = new byte[4];
            Serializer.Write(arr1, int.MinValue);
            var arr2 = BitConverter.GetBytes(int.MinValue);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Fact]
        public void WriteIntMaxTest()
        {
            var arr1 = new byte[4];
            Serializer.Write(arr1, int.MaxValue);
            var arr2 = BitConverter.GetBytes(int.MaxValue);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Fact]
        public void WriteLongTest()
        {
            var arr1 = new byte[8];
            Serializer.Write(arr1, 5L);
            var arr2 = BitConverter.GetBytes(5L);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Fact]
        public void WriteLongMinTest()
        {
            var arr1 = new byte[8];
            Serializer.Write(arr1, long.MinValue);
            var arr2 = BitConverter.GetBytes(long.MinValue);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Fact]
        public void WriteLongMaxTest()
        {
            var arr1 = new byte[8];
            Serializer.Write(arr1, long.MaxValue);
            var arr2 = BitConverter.GetBytes(long.MaxValue);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Fact]
        public void WriteULongTest()
        {
            var arr1 = new byte[8];
            Serializer.Write(arr1, 5ul);
            var arr2 = BitConverter.GetBytes(5ul);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Fact]
        public void WriteULongMinTest()
        {
            var arr1 = new byte[8];
            Serializer.Write(arr1, ulong.MinValue);
            var arr2 = BitConverter.GetBytes(ulong.MinValue);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Fact]
        public void WriteULongMaxTest()
        {
            var arr1 = new byte[8];
            Serializer.Write(arr1, ulong.MaxValue);
            var arr2 = BitConverter.GetBytes(ulong.MaxValue);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Fact]
        public void WriteDecimalTest()
        {
            var arr1 = new byte[16];
            Serializer.Write(arr1, 5m);
            var bits = decimal.GetBits(5);
            var arr2 = new byte[16];
            Array.Copy(BitConverter.GetBytes(bits[0]), 0, arr2, 0, 4);
            Array.Copy(BitConverter.GetBytes(bits[1]), 0, arr2, 4, 4);
            Array.Copy(BitConverter.GetBytes(bits[2]), 0, arr2, 8, 4);
            Array.Copy(BitConverter.GetBytes(bits[3]), 0, arr2, 12, 4);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Fact]
        public void WriteDecimalMinTest()
        {
            var arr1 = new byte[16];
            Serializer.Write(arr1, decimal.MinValue);
            var bits = decimal.GetBits(decimal.MinValue);
            var arr2 = new byte[16];
            Array.Copy(BitConverter.GetBytes(bits[0]), 0, arr2, 0, 4);
            Array.Copy(BitConverter.GetBytes(bits[1]), 0, arr2, 4, 4);
            Array.Copy(BitConverter.GetBytes(bits[2]), 0, arr2, 8, 4);
            Array.Copy(BitConverter.GetBytes(bits[3]), 0, arr2, 12, 4);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Fact]
        public void WriteDecimalMaxTest()
        {
            var arr1 = new byte[16];
            Serializer.Write(arr1, decimal.MaxValue);
            var bits = decimal.GetBits(decimal.MaxValue);
            var arr2 = new byte[16];
            Array.Copy(BitConverter.GetBytes(bits[0]), 0, arr2, 0, 4);
            Array.Copy(BitConverter.GetBytes(bits[1]), 0, arr2, 4, 4);
            Array.Copy(BitConverter.GetBytes(bits[2]), 0, arr2, 8, 4);
            Array.Copy(BitConverter.GetBytes(bits[3]), 0, arr2, 12, 4);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WriteBoolTest(bool data)
        {
            var arr1 = new byte[1];
            Serializer.Write(arr1, data);
            var arr2 = BitConverter.GetBytes(data);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReadBoolTest(bool data)
        {
            var arr = BitConverter.GetBytes(data);
            var actual = Serializer.ReadBool(arr);
            Assert.Equal(data, actual);
        }

        [Fact]
        public void ReadShortMinTest()
        {
            var arr = BitConverter.GetBytes(short.MinValue);
            var actual = Serializer.ReadShort(arr);
            Assert.Equal(short.MinValue, actual);
        }

        [Fact]
        public void ReadShortMaxTest()
        {
            var arr = BitConverter.GetBytes(short.MaxValue);
            var actual = Serializer.ReadShort(arr);
            Assert.Equal(short.MaxValue, actual);
        }

        [Fact]
        public void ReadShortTest()
        {
            var arr = BitConverter.GetBytes(5);
            var actual = Serializer.ReadShort(arr);
            Assert.Equal(5, actual);
        }

        [Fact]
        public void ReadIntMinTest()
        {
            var arr = BitConverter.GetBytes(int.MinValue);
            var actual = Serializer.ReadInt(arr);
            Assert.Equal(int.MinValue, actual);
        }

        [Fact]
        public void ReadIntMaxTest()
        {
            var arr = BitConverter.GetBytes(int.MaxValue);
            var actual = Serializer.ReadInt(arr);
            Assert.Equal(int.MaxValue, actual);
        }

        [Fact]
        public void ReadIntTest()
        {
            var arr = BitConverter.GetBytes(5);
            var actual = Serializer.ReadInt(arr);
            Assert.Equal(5, actual);
        }

        [Fact]
        public void ReadLongMinTest()
        {
            var arr = BitConverter.GetBytes(long.MinValue);
            var actual = Serializer.ReadLong(arr);
            Assert.Equal(long.MinValue, actual);
        }

        [Fact]
        public void ReadLongMaxTest()
        {
            var arr = BitConverter.GetBytes(long.MaxValue);
            var actual = Serializer.ReadLong(arr);
            Assert.Equal(long.MaxValue, actual);
        }

        [Fact]
        public void ReadLongTest()
        {
            var arr = BitConverter.GetBytes(5L);
            var actual = Serializer.ReadLong(arr);
            Assert.Equal(5L, actual);
        }

        [Fact]
        public void ReadULongMinTest()
        {
            var arr = BitConverter.GetBytes(ulong.MinValue);
            var actual = Serializer.ReadULong(arr);
            Assert.Equal(ulong.MinValue, actual);
        }

        [Fact]
        public void ReadULongMaxTest()
        {
            var arr = BitConverter.GetBytes(ulong.MaxValue);
            var actual = Serializer.ReadULong(arr);
            Assert.Equal(ulong.MaxValue, actual);
        }

        [Fact]
        public void ReadULongTest()
        {
            var arr = BitConverter.GetBytes(5ul);
            var actual = Serializer.ReadULong(arr);
            Assert.Equal(5ul, actual);
        }

        [Fact]
        public void ReadDecimalMinTest()
        {
            var arr = new byte[16];
            Serializer.Write(arr, decimal.MinValue);
            var actual = Serializer.ReadDecimal(arr);
            Assert.Equal(decimal.MinValue, actual);
        }

        [Fact]
        public void ReadDecimalMaxTest()
        {
            var arr = new byte[16];
            Serializer.Write(arr, decimal.MaxValue);
            var actual = Serializer.ReadDecimal(arr);
            Assert.Equal(decimal.MaxValue, actual);
        }

        [Fact]
        public void ReadDecimalTest()
        {
            var arr = new byte[16];
            Serializer.Write(arr, 5m);
            var actual = Serializer.ReadDecimal(arr);
            Assert.Equal(5m, actual);
        }

        [Fact]
        public void GetMessageType_ReturnsNullForLengthLess7()
        {
            Assert.Null(Serializer.GetMessageType(new byte[6]));
        }

        [Fact]
        public void GetMessageType_ReturnsIfSizeDoesNotMatchLength()
        {
            var bytes = new byte[10];
            Serializer.Write(bytes, bytes.Length);
            Assert.Null(Serializer.GetMessageType(bytes));
        }
    }
}