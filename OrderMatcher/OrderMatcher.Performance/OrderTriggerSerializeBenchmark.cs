using BenchmarkDotNet.Attributes;
using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System;
using System.Text.Json;

namespace OrderMatcher.Performance
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class OrderTriggerSerializeBenchmark
    {
        readonly OrderTrigger orderTrigger;

        public OrderTriggerSerializeBenchmark()
        {
            orderTrigger = new OrderTrigger { OrderId = 3453, Timestamp = 35345 };
        }

        [Benchmark(Baseline = true)]
        public void TriggerBinarySerialize()
        {
            Span<byte> bytes = stackalloc byte[OrderTriggerSerializer.MessageSize];
            OrderTriggerSerializer.Serialize(orderTrigger, bytes);
        }

        [Benchmark]
        public void TriggerJsonSerialize()
        {
            JsonSerializer.SerializeToUtf8Bytes(orderTrigger);
        }
    }
}
