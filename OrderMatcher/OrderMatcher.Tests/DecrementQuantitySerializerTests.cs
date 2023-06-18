namespace OrderMatcher.Tests;

public class DecrementQuantitySerializerTests
{
    private static readonly int messageSize = 47;

    [Fact]
    public void Serialize_Doesnotthrowexception_Min()
    {
        Span<byte> bytes = stackalloc byte[DecrementQuantitySerializer.MessageSize];
        DecrementQuantitySerializer.Serialize(new DecrementQuantity { OrderId = OrderId.MinValue, QuantityDecremented = Quantity.MinValue, UserId = UserId.MinValue, MessageSequence = long.MinValue }, bytes);
    }

    [Fact]
    public void Serialize_Doesnotthrowexception_Max()
    {
        Span<byte> bytes = stackalloc byte[DecrementQuantitySerializer.MessageSize];
        DecrementQuantitySerializer.Serialize(new DecrementQuantity { OrderId = OrderId.MaxValue, QuantityDecremented = Quantity.MaxValue, UserId = UserId.MaxValue, MessageSequence = long.MaxValue }, bytes);
    }

    [Fact]
    public void Serialize_ThrowsExecption_IfNullPassed()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => DecrementQuantitySerializer.Serialize(null, null));
        Assert.Equal("decrementQuanity", ex.ParamName);
    }

    [Fact]
    public void Deserialize_ThrowsExecption_IfMessageIsLessThan35Bytes()
    {
        var bytes = new byte[messageSize - 1];
        OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => DecrementQuantitySerializer.Deserialize(bytes));
        Assert.Equal($"Decrement quantity message must be of size : {messageSize}", ex.Message);
    }

    [Fact]
    public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
    {
        var bytes = new byte[messageSize + 1];
        OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => DecrementQuantitySerializer.Deserialize(bytes));
        Assert.Equal($"Decrement quantity message must be of size : {messageSize}", ex.Message);
    }

    [Fact]
    public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
    {
        var bytes = new byte[messageSize];
        OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => DecrementQuantitySerializer.Deserialize(bytes));
        Assert.Equal(Types.Constant.INVALID_MESSAGE, ex.Message);
    }

    [Fact]
    public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
    {
        var bytes = new byte[messageSize];
        bytes[4] = (byte)MessageType.DecrementQuantity;
        OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => DecrementQuantitySerializer.Deserialize(bytes));
        Assert.Equal(Types.Constant.INVALID_VERSION, ex.Message);
    }

    [Fact]
    public void Deserialize_Doesnotthrowexception_Min()
    {
        Span<byte> bytes = stackalloc byte[DecrementQuantitySerializer.MessageSize];
        DecrementQuantitySerializer.Serialize(new DecrementQuantity { OrderId = OrderId.MinValue, QuantityDecremented = Quantity.MinValue, UserId = UserId.MinValue, MessageSequence = long.MinValue }, bytes);
        var messageLength = BitConverter.ToInt32(bytes.Slice(0));
        Assert.Equal(messageSize, messageLength);
        var decrementQuanity = DecrementQuantitySerializer.Deserialize(bytes);
        Assert.Equal(OrderId.MinValue, decrementQuanity.OrderId);
        Assert.Equal(UserId.MinValue, decrementQuanity.UserId);
        Assert.Equal(long.MinValue, decrementQuanity.MessageSequence);
    }

    [Fact]
    public void Deserialize_Doesnotthrowexception_Max()
    {
        Span<byte> bytes = stackalloc byte[DecrementQuantitySerializer.MessageSize];
        DecrementQuantitySerializer.Serialize(new DecrementQuantity { OrderId = OrderId.MaxValue, QuantityDecremented = Quantity.MaxValue, UserId = UserId.MaxValue, MessageSequence = long.MaxValue }, bytes);

        var messageLength = BitConverter.ToInt32(bytes.Slice(0));
        Assert.Equal(messageSize, messageLength);
        var decrementQuanity = DecrementQuantitySerializer.Deserialize(bytes);
        Assert.Equal(OrderId.MaxValue, decrementQuanity.OrderId);
        Assert.Equal(UserId.MaxValue, decrementQuanity.UserId);
        Assert.Equal(long.MaxValue, decrementQuanity.MessageSequence);
    }

    [Fact]
    public void Deserialize_Doesnotthrowexception()
    {
        Span<byte> bytes = stackalloc byte[DecrementQuantitySerializer.MessageSize];
        DecrementQuantitySerializer.Serialize(new DecrementQuantity { OrderId = 12345678, QuantityDecremented = 89898123, UserId = 8768, MessageSequence = 79242 }, bytes);
        var messageLength = BitConverter.ToInt32(bytes.Slice(0));
        Assert.Equal(messageSize, messageLength);
        var decrementQuanity = DecrementQuantitySerializer.Deserialize(bytes);
        Assert.Equal((OrderId)12345678, decrementQuanity.OrderId);
        Assert.Equal((Quantity)89898123, decrementQuanity.QuantityDecremented);
        Assert.Equal((UserId)8768, decrementQuanity.UserId);
        Assert.Equal(79242, decrementQuanity.MessageSequence);
    }
}
