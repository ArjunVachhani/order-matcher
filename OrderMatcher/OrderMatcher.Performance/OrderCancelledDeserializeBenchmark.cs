namespace OrderMatcher.Performance;

[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class OrderCancelledDeserializeBenchmark
{
    readonly byte[] cancelJsonBytes;
    readonly byte[] cancelBinary;
    public OrderCancelledDeserializeBenchmark()
    {
        cancelJsonBytes = JsonSerializer.SerializeToUtf8Bytes(new CancelledOrder { OrderId = 1201, CancelReason = CancelReason.UserRequested, RemainingQuantity = 2000, Timestamp = 234 });
        cancelBinary = new byte[CancelledOrderSerializer.MessageSize];
        CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = 1201, CancelReason = CancelReason.UserRequested, RemainingQuantity = 2000, Timestamp = 234 }, cancelBinary);
    }

    [Benchmark]
    public void CancelJsonDeserialize()
    {
        JsonSerializer.Deserialize<CancelledOrder>(cancelJsonBytes);
    }

    [Benchmark(Baseline = true)]
    public void CancelBinaryDeserialize()
    {
        CancelledOrderSerializer.Deserialize(cancelBinary);
    }
}
