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
            Serializer.Write(arr1, 5);
            var arr2 = BitConverter.GetBytes((long)5);
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
            Serializer.Write(arr1, 5);
            var arr2 = BitConverter.GetBytes((ulong)5);
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
            Serializer.Write(arr1, 5);
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

        [Fact]
        public void WriteboolMinTest()
        {
            var arr1 = new byte[1];
            Serializer.Write(arr1, true);
            var arr2 = BitConverter.GetBytes(true);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Fact]
        public void WriteboolMaxTest()
        {
            var arr1 = new byte[1];
            Serializer.Write(arr1, false);
            var arr2 = BitConverter.GetBytes(false);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        [Fact]
        public void GetMessageType_ThrowsExceptionForNullArguement()
        {
            Assert.Throws<ArgumentNullException>(() => Serializer.GetMessageType(null));
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