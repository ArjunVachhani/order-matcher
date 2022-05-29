using BenchmarkDotNet.Attributes;
using MessagePack;
using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System;
using System.Text.Json;

namespace OrderMatcher.Performance
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class OrderSerializeBenchmark
    {
        readonly Order order;
        readonly Order2 order2;
        public OrderSerializeBenchmark()
        {
            order2 = new Order2 { IsBuy = true, IsTip = false, OpenQuantity = 100, OrderId = 1001, Price = 400, Quantity = 100, Sequnce = 0, StopPrice = 0 };
            order = new Order { IsBuy = true, OpenQuantity = 100, OrderId = 1001, Price = 400, Sequence = 0, TotalQuantity = 100, OrderCondition = OrderCondition.None, StopPrice = 0 };
        }

        [Benchmark]
        public void OrderJsonSerialize()
        {
            JsonSerializer.SerializeToUtf8Bytes(order);
        }

        [Benchmark]
        public void OrderMsgPckSerialize()
        {
            MessagePackSerializer.Serialize(order2);
        }

        [Benchmark(Baseline = true)]
        public void OrderBinarySerialize()
        {
            Span<byte> bytes = stackalloc byte[OrderSerializer.MessageSize];
            OrderSerializer.Serialize(order, bytes);
        }
    }
}
