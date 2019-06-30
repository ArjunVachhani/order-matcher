using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace OrderMatcher.Performance
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class SerializeBenchmark
    {
        Book book;
        List<KeyValuePair<Price, Quantity>> bid;
        List<KeyValuePair<Price, Quantity>> ask;

        string orderJsonString;
        byte[] orderBinarySerialized;

        string fillJsonString;
        byte[] fillBinary;

        string cancelJsonString;
        byte[] cancelBinary;

        string cancelRequestJsonString;
        byte[] cancelRequestBinary;

        string orderTriggerJsonString;
        byte[] orderTriggerBinary;

        string bookRequestJsonString;
        byte[] bookRequestBinary;

        string bookJsonString;
        byte[] bookBinary;

        public SerializeBenchmark()
        {
            book = new Book();
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = true, IsTip = false, OpenQuantity = 1000, OrderCondition = OrderCondition.None, OrderId = 3434, Price = 234, Quantity = 1000, StopPrice = 0, TotalQuantity = 0 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = true, IsTip = false, OpenQuantity = 1000, OrderCondition = OrderCondition.None, OrderId = 3434, Price = 235, Quantity = 1000, StopPrice = 0, TotalQuantity = 0 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = true, IsTip = false, OpenQuantity = 1000, OrderCondition = OrderCondition.None, OrderId = 3435, Price = 236, Quantity = 1000, StopPrice = 0, TotalQuantity = 0 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = true, IsTip = false, OpenQuantity = 1000, OrderCondition = OrderCondition.None, OrderId = 3436, Price = 237, Quantity = 1000, StopPrice = 0, TotalQuantity = 0 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = true, IsTip = false, OpenQuantity = 1000, OrderCondition = OrderCondition.None, OrderId = 3437, Price = 238, Quantity = 1000, StopPrice = 0, TotalQuantity = 0 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = false, IsTip = false, OpenQuantity = 1000, OrderCondition = OrderCondition.None, OrderId = 3438, Price = 239, Quantity = 1000, StopPrice = 0, TotalQuantity = 0 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = false, IsTip = false, OpenQuantity = 1000, OrderCondition = OrderCondition.None, OrderId = 3439, Price = 240, Quantity = 1000, StopPrice = 0, TotalQuantity = 0 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = false, IsTip = false, OpenQuantity = 1000, OrderCondition = OrderCondition.None, OrderId = 3440, Price = 241, Quantity = 1000, StopPrice = 0, TotalQuantity = 0 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = false, IsTip = false, OpenQuantity = 1000, OrderCondition = OrderCondition.None, OrderId = 3441, Price = 242, Quantity = 1000, StopPrice = 0, TotalQuantity = 0 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = false, IsTip = false, OpenQuantity = 1000, OrderCondition = OrderCondition.None, OrderId = 3442, Price = 243, Quantity = 1000, StopPrice = 0, TotalQuantity = 0 });
            bid = book.BidSide.Select(x => new KeyValuePair<Price, Quantity>(x.Key, x.Value.Quantity)).ToList();
            ask = book.AskSide.Select(x => new KeyValuePair<Price, Quantity>(x.Key, x.Value.Quantity)).ToList();


            orderJsonString = JsonConvert.SerializeObject(new Order { CancelOn = 12345678, IsBuy = true, OrderCondition = OrderCondition.ImmediateOrCancel, OrderId = 56789, Price = 404, Quantity = 2356, StopPrice = 9534, TotalQuantity = 7878234 });
            orderBinarySerialized = OrderSerializer.Serialize(new Order { CancelOn = 12345678, IsBuy = true, OrderCondition = OrderCondition.ImmediateOrCancel, OrderId = 56789, Price = 404, Quantity = 2356, StopPrice = 9534, TotalQuantity = 7878234 });

            fillJsonString = JsonConvert.SerializeObject(new Fill { MakerOrderId = 10001, MatchQuantity = 2000, MatchRate = 2400, TakerOrderId = 9999, Timestamp = 10303 });
            fillBinary = FillSerializer.Serialize(new Fill { MakerOrderId = 10001, MatchQuantity = 2000, MatchRate = 2400, TakerOrderId = 9999, Timestamp = 10303 });

            cancelJsonString = JsonConvert.SerializeObject(new CancelledOrder { OrderId = 1201, CancelReason = CancelReason.UserRequested, RemainingQuantity = 2000, Timestamp = 234 });
            cancelBinary = CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = 1201, CancelReason = CancelReason.UserRequested, RemainingQuantity = 2000, Timestamp = 234 });

            cancelRequestJsonString = JsonConvert.SerializeObject(new CancelRequest { OrderId = 1023 });
            cancelRequestBinary = CancelRequestSerializer.Serialize(new CancelRequest { OrderId = 1023 });

            orderTriggerBinary = TriggerSerializer.Serialize(new OrderTrigger { OrderId = 3453, Timestamp = 35345 });
            orderTriggerJsonString = JsonConvert.SerializeObject(new OrderTrigger { OrderId = 3453, Timestamp = 35345 });

            bookRequestBinary = BookRequestSerializer.Serialize(new BookRequest { });
            bookRequestJsonString = JsonConvert.SerializeObject(new BookRequest { });

            bookJsonString = JsonConvert.SerializeObject(new BookDepth { Bid = bid, Ask = ask, LTP = 100, TimeStamp = 1234 });
            bookBinary = BookSerializer.Serialize(book, 5, 100, 1234);
        }

        [Benchmark]
        public void orderJsonDeserialize()
        {
            var order = JsonConvert.DeserializeObject<Order>(orderJsonString);
        }

        [Benchmark]
        public void orderBinaryDeserialize()
        {
            var order = OrderSerializer.Deserialize(orderBinarySerialized);
        }

        [Benchmark]
        public void fillJsonDeserialize()
        {
            var fill = JsonConvert.DeserializeObject<Fill>(fillJsonString);
        }

        [Benchmark]
        public void fillBinaryDeserialize()
        {
            var fill = FillSerializer.Deserialize(fillBinary);
        }

        [Benchmark]
        public void cancelJsonDeserialize()
        {
            var cancel = JsonConvert.DeserializeObject<CancelledOrder>(cancelJsonString);
        }

        [Benchmark]
        public void cancelBinaryDeserialize()
        {
            var cancel = CancelledOrderSerializer.Deserialize(cancelBinary);
        }

        [Benchmark]
        public void orderTriggerJsonDeserialize()
        {
            var trigger = JsonConvert.DeserializeObject<OrderTrigger>(orderTriggerJsonString);
        }

        [Benchmark]
        public void orderTriggerBinaryDeserialize()
        {
            var trigger = TriggerSerializer.Deserialize(orderTriggerBinary);
        }

        [Benchmark]
        public void cancelRequestJsonDeserialize()
        {
            var cancelrequest = JsonConvert.DeserializeObject<CancelRequest>(cancelRequestJsonString);
        }

        [Benchmark]
        public void cancelRequestBinaryDeserialize()
        {
            var cancelRequest = CancelRequestSerializer.Deserialize(cancelRequestBinary);
        }

        [Benchmark]
        public void bookRequestJsonDeserialize()
        {
            var bookrequest = JsonConvert.DeserializeObject<BookRequest>(bookRequestJsonString);
        }

        [Benchmark]
        public void bookRequestBinaryDeserialize()
        {
            var bookrequest = BookRequestSerializer.Deserialize(bookRequestBinary);
        }

        [Benchmark]
        public void bookJsonDeserialize()
        {
            var bookDepth = JsonConvert.DeserializeObject<BookDepth>(bookJsonString);
        }

        [Benchmark]
        public void bookBinaryDeserialize()
        {
            var bookDepth = BookSerializer.Deserialize(bookBinary);
        }

        [Benchmark]
        public void orderJsonSerialize()
        {
            var orderJsonString = JsonConvert.SerializeObject(new Order { IsBuy = true, IsTip = false, OpenQuantity = 100, OrderCondition = OrderCondition.None, OrderId = 1001, Price = 400, Quantity = 100, Sequnce = 0, StopPrice = 0 });
        }

        [Benchmark]
        public void orderBinarySerialize()
        {
            var bytes = OrderSerializer.Serialize(new Order { IsBuy = true, IsTip = false, OpenQuantity = 100, OrderCondition = OrderCondition.None, OrderId = 1001, Price = 400, Quantity = 100, Sequnce = 0, StopPrice = 0 });
        }

        [Benchmark]
        public void orderBinarySerializeOptimized()
        {
            var bytes = OrderSerializer.SerializeOptimized(new Order { IsBuy = true, IsTip = false, OpenQuantity = 100, OrderCondition = OrderCondition.None, OrderId = 1001, Price = 400, Quantity = 100, Sequnce = 0, StopPrice = 0 });
        }

        [Benchmark]
        public void fillJsonSerialize()
        {
            var fill = JsonConvert.SerializeObject(new Fill { MakerOrderId = 10001, MatchQuantity = 2000, MatchRate = 2400, TakerOrderId = 9999, Timestamp = 10303 });
        }

        [Benchmark]
        public void fillBinarySerialize()
        {
            var bytes = FillSerializer.Serialize(new Fill { MakerOrderId = 10001, MatchQuantity = 2000, MatchRate = 2400, TakerOrderId = 9999, Timestamp = 10303 });
        }

        [Benchmark]
        public void cancelJsonSerialize()
        {
            var cancel = JsonConvert.SerializeObject(new CancelledOrder { OrderId = 1201, CancelReason = CancelReason.UserRequested, RemainingQuantity = 2000, Timestamp = 234 });
        }

        [Benchmark]
        public void cancelBinarySerialize()
        {
            var bytes = CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = 1201, CancelReason = CancelReason.UserRequested, RemainingQuantity = 2000, Timestamp = 234 });
        }

        [Benchmark]
        public void cancelRequestJsonSerialize()
        {
            var bytes = JsonConvert.SerializeObject(new CancelRequest { OrderId = 1023 });
        }

        [Benchmark]
        public void cancelRequestBinarySerialize()
        {
            var bytes = CancelRequestSerializer.Serialize(new CancelRequest { OrderId = 1023 });
        }

        [Benchmark]
        public void triggerBinarySerialize()
        {
            var msg = TriggerSerializer.Serialize(new OrderTrigger { OrderId = 3453, Timestamp = 35345 });
        }

        [Benchmark]
        public void triggerJsonSerialize()
        {
            var msg = JsonConvert.SerializeObject(new OrderTrigger { OrderId = 3453, Timestamp = 35345 });
        }

        [Benchmark]
        public void bookRequestBinarySerialize()
        {
            var msg = BookRequestSerializer.Serialize(new BookRequest { });
        }

        [Benchmark]
        public void bookRequestJsonSerialize()
        {
            var msg = JsonConvert.SerializeObject(new BookRequest { });
        }

        [Benchmark]
        public void bookSerializeBinarySerialize()
        {
            var msg = BookSerializer.Serialize(book, 50, 100, 1234);
        }

        [Benchmark]
        public void bookJsonSerialize()
        {
            var msg = JsonConvert.SerializeObject(new BookDepth { Bid = bid, Ask = ask, LTP = 100, TimeStamp = 1234 });
        }
    }
}
