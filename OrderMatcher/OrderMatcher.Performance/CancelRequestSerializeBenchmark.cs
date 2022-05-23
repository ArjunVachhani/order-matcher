using BenchmarkDotNet.Attributes;
using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System;
using System.Text.Json;

namespace OrderMatcher.Performance
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class CancelRequestSerializeBenchmark
    {
        readonly CancelRequest cancelRequest;
        public CancelRequestSerializeBenchmark()
        {
            cancelRequest = new CancelRequest { OrderId = 1023 };
        }

        [Benchmark]
        public void CancelRequestJsonSerialize()
        {
            JsonSerializer.SerializeToUtf8Bytes(cancelRequest);
        }

        [Benchmark(Baseline = true)]
        public void CancelRequestBinarySerialize()
        {
            Span<byte> bytes = stackalloc byte[CancelRequestSerializer.MessageSize];
            CancelRequestSerializer.Serialize(cancelRequest, bytes);
        }
    }
}
