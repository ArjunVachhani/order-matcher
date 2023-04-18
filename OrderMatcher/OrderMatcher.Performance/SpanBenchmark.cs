namespace OrderMatcher.Performance;

[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class SpanBenchmark
{
    [Benchmark]
    public void Slice()
    {
        Span<byte> bytes = stackalloc byte[10];
        bytes.Slice(3);
    }

    [Benchmark]
    public void SliceWithLength()
    {
        Span<byte> bytes = stackalloc byte[10];
        bytes.Slice(3, 4);
    }
}
