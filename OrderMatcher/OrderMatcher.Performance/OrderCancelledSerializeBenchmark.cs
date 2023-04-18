namespace OrderMatcher.Performance;

[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class OrderCancelledSerializeBenchmark
{
    readonly CancelledOrder cancelledOrder;

    public OrderCancelledSerializeBenchmark()
    {
        cancelledOrder = new CancelledOrder { OrderId = 1201, CancelReason = CancelReason.UserRequested, RemainingQuantity = 2000, Timestamp = 234 };
    }

    [Benchmark]
    public void CancelJsonSerialize()
    {
        JsonSerializer.SerializeToUtf8Bytes(cancelledOrder);
    }

    [Benchmark(Baseline = true)]
    public void CancelBinarySerialize()
    {
        Span<byte> bytes = stackalloc byte[CancelledOrderSerializer.MessageSize];
        CancelledOrderSerializer.Serialize(cancelledOrder, bytes);
    }
}
