using BenchmarkDotNet.Attributes;
using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System.Text.Json;

namespace OrderMatcher.Performance
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class OrderTriggerDeserializeBenchmark
    {
        readonly byte[] orderTriggerJsonBytes;
        readonly byte[] orderTriggerBinary;

        public OrderTriggerDeserializeBenchmark()
        {
            orderTriggerBinary = new byte[OrderTriggerSerializer.MessageSize];
            OrderTriggerSerializer.Serialize(new OrderTrigger { OrderId = 3453, Timestamp = 35345 }, orderTriggerBinary);
            orderTriggerJsonBytes = JsonSerializer.SerializeToUtf8Bytes(new OrderTrigger { OrderId = 3453, Timestamp = 35345 });
        }

        [Benchmark]
        public void OrderTriggerJsonDeserialize()
        {
            JsonSerializer.Deserialize<OrderTrigger>(orderTriggerJsonBytes);
        }

        [Benchmark(Baseline = true)]
        public void OrderTriggerBinaryDeserialize()
        {
            OrderTriggerSerializer.Deserialize(orderTriggerBinary);
        }
    }
}
