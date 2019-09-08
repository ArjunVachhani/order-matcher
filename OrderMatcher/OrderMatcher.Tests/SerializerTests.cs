using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class SerializerTests
    {
        [Fact]
        public void WriteIntTest()
        {
            var arr1 = new byte[4];
            Serializer.Write(arr1, 0, 5);
            var arr2 = BitConverter.GetBytes(5);
            AssertHelper.SequentiallyEqual(arr1, arr2);
        }

        //TODO write remaining unit test
    }
}