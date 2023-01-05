using OrderMatcher.Types;
using Xunit;

namespace OrderMatcher.Tests
{
    public class QuantityTests
    {
        [Fact]
        public void WriteBytesMinTest()
        {
            var bytes = new byte[Quantity.SizeOfQuantity];
            Quantity.MinValue.WriteBytes(bytes);
            var quantity = Quantity.ReadQuantity(bytes);
            Assert.Equal(Quantity.MinValue, quantity);
        }

        [Fact]
        public void WriteBytesMaxTest()
        {
            var bytes = new byte[Quantity.SizeOfQuantity];
            Quantity.MaxValue.WriteBytes(bytes);
            var quantity = Quantity.ReadQuantity(bytes);
            Assert.Equal(Quantity.MaxValue, quantity);
        }

        [Fact]
        public void WriteBytesTest()
        {
            var bytes = new byte[Quantity.SizeOfQuantity];
            var quantity = new Quantity(10);
            quantity.WriteBytes(bytes);
            var actualQuantity = Quantity.ReadQuantity(bytes);
            Assert.Equal(quantity, actualQuantity);
        }

        [Fact]
        public void StaticWriteBytesMinTest()
        {
            var bytes = new byte[Quantity.SizeOfQuantity];
            Quantity.WriteBytes(bytes, Quantity.MinValue);
            var quantity = Quantity.ReadQuantity(bytes);
            Assert.Equal(Quantity.MinValue, quantity);
        }

        [Fact]
        public void StaticWriteBytesMaxTest()
        {
            var bytes = new byte[Quantity.SizeOfQuantity];
            Quantity.WriteBytes(bytes, Quantity.MaxValue);
            var quantity = Quantity.ReadQuantity(bytes);
            Assert.Equal(Quantity.MaxValue, quantity);
        }

        [Fact]
        public void StaticWriteBytesTest()
        {
            var bytes = new byte[Quantity.SizeOfQuantity];
            var quantity = new Quantity(10);
            Quantity.WriteBytes(bytes, quantity);
            var actualQuantity = Quantity.ReadQuantity(bytes);
            Assert.Equal(quantity, actualQuantity);
        }
    }
}
