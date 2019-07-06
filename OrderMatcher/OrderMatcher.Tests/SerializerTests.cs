using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class SerializerTests
    {
        [Fact]
        public void WriteIntTest()
        {
            byte[] arr = new byte[4];
            Serializer.WriteInt(arr, 0, 5);
            var c = arr [0];
        }
    }
}