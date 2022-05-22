using BenchmarkDotNet.Attributes;
using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System.Text.Json;

namespace OrderMatcher.Performance
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class CancelRequestDeserializeBenchmark
    {
        readonly byte[] cancelRequestJsonBytes;
        readonly byte[] cancelRequestBinary;

        public CancelRequestDeserializeBenchmark()
        {
            cancelRequestJsonBytes = JsonSerializer.SerializeToUtf8Bytes(new CancelRequest { OrderId = 1023 });
            cancelRequestBinary = new byte[CancelRequestSerializer.MessageSize];
            CancelRequestSerializer.Serialize(new CancelRequest { OrderId = 1023 }, cancelRequestBinary);
        }

        [Benchmark]
        public void CancelRequestJsonDeserialize()
        {
            JsonSerializer.Deserialize<CancelRequest>(cancelRequestJsonBytes);
        }

        [Benchmark(Baseline = true)]
        public void CancelRequestBinaryDeserialize()
        {
            CancelRequestSerializer.Deserialize(cancelRequestBinary);
        }
    }
}
