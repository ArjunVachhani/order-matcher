namespace OrderMatcher.Tests;

public class PriceTests
{
    [Fact]
    public void WriteBytesMinTest()
    {
        var bytes = new byte[Price.SizeOfPrice];
        Price.MinValue.WriteBytes(bytes);
        var price = Price.ReadPrice(bytes);
        Assert.Equal(Price.MinValue, price);
    }

    [Fact]
    public void WriteBytesMaxTest()
    {
        var bytes = new byte[Price.SizeOfPrice];
        Price.MaxValue.WriteBytes(bytes);
        var price = Price.ReadPrice(bytes);
        Assert.Equal(Price.MaxValue, price);
    }

    [Fact]
    public void WriteBytesTest()
    {
        var bytes = new byte[Price.SizeOfPrice];
        var price = new Price(10);
        price.WriteBytes(bytes);
        var actualPrice = Price.ReadPrice(bytes);
        Assert.Equal(price, actualPrice);
    }

    [Fact]
    public void StaticWriteBytesMinTest()
    {
        var bytes = new byte[Price.SizeOfPrice];
        Price.WriteBytes(bytes, Price.MinValue);
        var price = Price.ReadPrice(bytes);
        Assert.Equal(Price.MinValue, price);
    }

    [Fact]
    public void StaticWriteBytesMaxTest()
    {
        var bytes = new byte[Price.SizeOfPrice];
        Price.WriteBytes(bytes, Price.MaxValue);
        var price = Price.ReadPrice(bytes);
        Assert.Equal(Price.MaxValue, price);
    }

    [Fact]
    public void StaticWriteBytesTest()
    {
        var bytes = new byte[Price.SizeOfPrice];
        var price = new Price(10);
        Price.WriteBytes(bytes, price);
        var actualPrice = Price.ReadPrice(bytes);
        Assert.Equal(price, actualPrice);
    }
}
