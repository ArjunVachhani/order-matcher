namespace OrderMatcher.Performance;

[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class FillDeserializeBenchmark
{
    readonly byte[] fillJsonBytes;
    readonly byte[] fillBinary;
    public FillDeserializeBenchmark()
    {
        fillJsonBytes = JsonSerializer.SerializeToUtf8Bytes(new Fill { MakerOrderId = 10001, MatchQuantity = 2000, MatchRate = 2400, TakerOrderId = 9999, Timestamp = 10303 });
        fillBinary = new byte[FillSerializer.MessageSize];
        FillSerializer.Serialize(new Fill { MakerOrderId = 10001, MatchQuantity = 2000, MatchRate = 2400, TakerOrderId = 9999, Timestamp = 10303 }, fillBinary);
    }

    [Benchmark]
    public void FillJsonDeserialize()
    {
        JsonSerializer.Deserialize<Fill>(fillJsonBytes);
    }

    [Benchmark(Baseline = true)]
    public void FillBinaryDeserialize()
    {
        FillSerializer.Deserialize(fillBinary);
    }
}
