﻿namespace OrderMatcher.Tests;

public class MatchingEngineResultSerializerTests
{
    [Fact]
    public void Serialize_Doesnotthrowexception_Min()
    {
        Span<byte> bytes = stackalloc byte[MatchingEngineResultSerializer.MessageSize];
        MatchingEngineResultSerializer.Serialize(new MatchingEngineResult { OrderId = UInt64.MinValue, Result = OrderMatchingResult.OrderAccepted, Timestamp = long.MinValue }, bytes);
    }

    [Fact]
    public void Serialize_Doesnotthrowexception_Max()
    {
        Span<byte> bytes = stackalloc byte[MatchingEngineResultSerializer.MessageSize];
        MatchingEngineResultSerializer.Serialize(new MatchingEngineResult { OrderId = UInt64.MaxValue, Result = OrderMatchingResult.QuantityAndTotalQuantityShouldBeMultipleOfStepSize, Timestamp = long.MaxValue }, bytes);
    }

    [Fact]
    public void Serialize_ThrowsExecption_IfNullPassed()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => MatchingEngineResultSerializer.Serialize(null, null));
        Assert.Equal("matchingEngineResult", ex.ParamName);
    }

    [Fact]
    public void Deserialize_ThrowsExecption_IfMessageIsLessThan35Bytes()
    {
        var bytes = new byte[23];
        OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => MatchingEngineResultSerializer.Deserialize(bytes));
        Assert.Equal("OrderMatchingResult Message must be of Size : 24", ex.Message);
    }

    [Fact]
    public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
    {
        var bytes = new byte[25];
        OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => MatchingEngineResultSerializer.Deserialize(bytes));
        Assert.Equal("OrderMatchingResult Message must be of Size : 24", ex.Message);
    }

    [Fact]
    public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
    {
        var bytes = new byte[24];
        OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => MatchingEngineResultSerializer.Deserialize(bytes));
        Assert.Equal(Types.Constant.INVALID_MESSAGE, ex.Message);
    }

    [Fact]
    public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
    {
        var bytes = new byte[24];
        bytes[4] = (byte)MessageType.OrderMatchingResult;
        OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => MatchingEngineResultSerializer.Deserialize(bytes));
        Assert.Equal(Types.Constant.INVALID_VERSION, ex.Message);
    }

    [Fact]
    public void Deserialize_Doesnotthrowexception_Min()
    {
        Span<byte> bytes = stackalloc byte[MatchingEngineResultSerializer.MessageSize];
        MatchingEngineResultSerializer.Serialize(new MatchingEngineResult { OrderId = UInt64.MinValue, Result = OrderMatchingResult.OrderAccepted, Timestamp = long.MinValue }, bytes);
        var messageLength = BitConverter.ToInt32(bytes.Slice(0));
        Assert.Equal(24, messageLength);
        var result = MatchingEngineResultSerializer.Deserialize(bytes);
        Assert.Equal(ulong.MinValue, result.OrderId);
        Assert.Equal(OrderMatchingResult.OrderAccepted, result.Result);
        Assert.Equal(long.MinValue, result.Timestamp);
    }

    [Fact]
    public void Deserialize_Doesnotthrowexception_Max()
    {
        Span<byte> bytes = stackalloc byte[MatchingEngineResultSerializer.MessageSize];
        MatchingEngineResultSerializer.Serialize(new MatchingEngineResult { OrderId = UInt64.MaxValue, Result = OrderMatchingResult.QuantityAndTotalQuantityShouldBeMultipleOfStepSize, Timestamp = long.MaxValue }, bytes);
        var messageLength = BitConverter.ToInt32(bytes.Slice(0));
        Assert.Equal(24, messageLength);
        var result = MatchingEngineResultSerializer.Deserialize(bytes);
        Assert.Equal(ulong.MaxValue, result.OrderId);
        Assert.Equal(OrderMatchingResult.QuantityAndTotalQuantityShouldBeMultipleOfStepSize, result.Result);
        Assert.Equal(long.MaxValue, result.Timestamp);
    }

    [Fact]
    public void Deserialize_Doesnotthrowexception()
    {
        Span<byte> bytes = stackalloc byte[MatchingEngineResultSerializer.MessageSize];
        MatchingEngineResultSerializer.Serialize(new MatchingEngineResult { OrderId = 16879, Result = OrderMatchingResult.IcebergOrderCannotBeMarketOrStopMarketOrder, Timestamp = 132465 }, bytes);
        var messageLength = BitConverter.ToInt32(bytes.Slice(0));
        Assert.Equal(24, messageLength);
        var result = MatchingEngineResultSerializer.Deserialize(bytes);
        Assert.Equal((ulong)16879, result.OrderId);
        Assert.Equal(OrderMatchingResult.IcebergOrderCannotBeMarketOrStopMarketOrder, result.Result);
        Assert.Equal(132465, result.Timestamp);
    }
}
