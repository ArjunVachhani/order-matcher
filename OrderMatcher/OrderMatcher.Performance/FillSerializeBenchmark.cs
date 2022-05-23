using BenchmarkDotNet.Attributes;
using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System;
using System.Text.Json;

namespace OrderMatcher.Performance
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class FillSerializeBenchmark
    {
        readonly Fill fill;
        public FillSerializeBenchmark()
        {
            fill = new Fill { MakerOrderId = 10001, MatchQuantity = 2000, MatchRate = 2400, TakerOrderId = 9999, Timestamp = 10303 };
        }

        [Benchmark]
        public void FillJsonSerialize()
        {
            JsonSerializer.SerializeToUtf8Bytes(fill);
        }

        [Benchmark(Baseline = true)]
        public void FillBinarySerialize()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(fill, bytes);
        }
    }
}
