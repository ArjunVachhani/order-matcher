namespace OrderMatcher.Tests;

public class SelfMatchSerializersTests
{
    private static readonly int messageSize = 47;

    [Fact]
    public void Serialize_Doesnotthrowexception_Min()
    {
        Span<byte> bytes = stackalloc byte[SelfMatchSerializer.MessageSize];
        SelfMatchSerializer.Serialize(new SelfMatch { IncomingOrderId = OrderId.MinValue, RestingOrderId = OrderId.MinValue, UserId = UserId.MinValue, MessageSequence = long.MinValue }, bytes);
    }

    [Fact]
    public void Serialize_Doesnotthrowexception_Max()
    {
        Span<byte> bytes = stackalloc byte[SelfMatchSerializer.MessageSize];
        SelfMatchSerializer.Serialize(new SelfMatch { IncomingOrderId = OrderId.MaxValue, RestingOrderId = OrderId.MaxValue, UserId = UserId.MaxValue, MessageSequence = long.MaxValue }, bytes);
    }

    [Fact]
    public void Serialize_ThrowsExecption_IfNullPassed()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => SelfMatchSerializer.Serialize(null, null));
        Assert.Equal("selfMatch", ex.ParamName);
    }

    [Fact]
    public void Deserialize_ThrowsExecption_IfMessageIsLessThan35Bytes()
    {
        var bytes = new byte[messageSize - 1];
        OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => SelfMatchSerializer.Deserialize(bytes));
        Assert.Equal($"Self match message must be of size : {messageSize}", ex.Message);
    }

    [Fact]
    public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
    {
        var bytes = new byte[messageSize + 1];
        OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => SelfMatchSerializer.Deserialize(bytes));
        Assert.Equal($"Self match message must be of size : {messageSize}", ex.Message);
    }

    [Fact]
    public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
    {
        var bytes = new byte[messageSize];
        OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => SelfMatchSerializer.Deserialize(bytes));
        Assert.Equal(Types.Constant.INVALID_MESSAGE, ex.Message);
    }

    [Fact]
    public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
    {
        var bytes = new byte[messageSize];
        bytes[4] = (byte)MessageType.SelfMatch;
        OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => SelfMatchSerializer.Deserialize(bytes));
        Assert.Equal(Types.Constant.INVALID_VERSION, ex.Message);
    }

    [Fact]
    public void Deserialize_Doesnotthrowexception_Min()
    {
        Span<byte> bytes = stackalloc byte[SelfMatchSerializer.MessageSize];
        SelfMatchSerializer.Serialize(new SelfMatch { IncomingOrderId = OrderId.MinValue, RestingOrderId = OrderId.MinValue, UserId = UserId.MinValue, MessageSequence = long.MinValue }, bytes);
        var messageLength = BitConverter.ToInt32(bytes.Slice(0));
        Assert.Equal(messageSize, messageLength);
        var selfMatch = SelfMatchSerializer.Deserialize(bytes);
        Assert.Equal(OrderId.MinValue, selfMatch.IncomingOrderId);
        Assert.Equal(UserId.MinValue, selfMatch.UserId);
        Assert.Equal(long.MinValue, selfMatch.MessageSequence);
    }

    [Fact]
    public void Deserialize_Doesnotthrowexception_Max()
    {
        Span<byte> bytes = stackalloc byte[SelfMatchSerializer.MessageSize];
        SelfMatchSerializer.Serialize(new SelfMatch { IncomingOrderId = OrderId.MaxValue, RestingOrderId = OrderId.MaxValue, UserId = UserId.MaxValue, MessageSequence = long.MaxValue }, bytes);

        var messageLength = BitConverter.ToInt32(bytes.Slice(0));
        Assert.Equal(messageSize, messageLength);
        var selfMatch = SelfMatchSerializer.Deserialize(bytes);
        Assert.Equal(OrderId.MaxValue, selfMatch.IncomingOrderId);
        Assert.Equal(UserId.MaxValue, selfMatch.UserId);
        Assert.Equal(long.MaxValue, selfMatch.MessageSequence);
    }

    [Fact]
    public void Deserialize_Doesnotthrowexception()
    {
        Span<byte> bytes = stackalloc byte[SelfMatchSerializer.MessageSize];
        SelfMatchSerializer.Serialize(new SelfMatch { IncomingOrderId = 12345678, RestingOrderId = 89898123, UserId = 8768, MessageSequence = 79242 }, bytes);
        var messageLength = BitConverter.ToInt32(bytes.Slice(0));
        Assert.Equal(messageSize, messageLength);
        var selfMatch = SelfMatchSerializer.Deserialize(bytes);
        Assert.Equal((OrderId)12345678, selfMatch.IncomingOrderId);
        Assert.Equal((OrderId)89898123, selfMatch.RestingOrderId);
        Assert.Equal((UserId)8768, selfMatch.UserId);
        Assert.Equal(79242, selfMatch.MessageSequence);
    }
}
