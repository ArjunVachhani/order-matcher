namespace OrderMatcher.Performance;

[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class OrderDeserializeBenchmark
{
    readonly byte[] orderBinarySerialized;
    readonly byte[] orderJsonBytes;
    readonly byte[] orderMsgPck;

    public OrderDeserializeBenchmark()
    {
        var order = new Order() { CancelOn = 12345678, IsBuy = true, OrderId = 56789, Price = 404, OpenQuantity = 1000, OrderCondition = OrderCondition.ImmediateOrCancel, StopPrice = 9534, TotalQuantity = 7878234 };
        orderJsonBytes = JsonSerializer.SerializeToUtf8Bytes(order);

        orderBinarySerialized = new byte[OrderSerializer.MessageSize];
        OrderSerializer.Serialize(order, orderBinarySerialized);

        orderMsgPck = MessagePackSerializer.Serialize(new Order2 { IsBuy = true, IsTip = false, OpenQuantity = 100, OrderId = 1001, Price = 400, Quantity = 100, Sequnce = 0, StopPrice = 0 });
    }

    [Benchmark]
    public void OrderJsonDeserialize()
    {
        JsonSerializer.Deserialize<Order>(orderJsonBytes);
    }

    [Benchmark]
    public void OrderMsgpckDeserialize()
    {
        MessagePackSerializer.Deserialize<Order2>(orderMsgPck);
    }

    [Benchmark(Baseline = true)]
    public void OrderBinaryDeserialize()
    {
        OrderSerializer.Deserialize(orderBinarySerialized);
    }
}
