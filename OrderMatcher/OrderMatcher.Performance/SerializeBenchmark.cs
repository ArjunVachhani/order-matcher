using BenchmarkDotNet.Attributes;
using MessagePack;
using Newtonsoft.Json;
using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderMatcher.Performance
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class SerializeBenchmark
    {
        readonly Book book;
        readonly Dictionary<Price, Quantity> bid;
        readonly Dictionary<Price, Quantity> ask;
        readonly string orderJsonString;
        readonly byte[] orderBinarySerialized;
        readonly string fillJsonString;
        readonly byte[] fillBinary;
        readonly string cancelJsonString;
        readonly byte[] cancelBinary;
        readonly string cancelRequestJsonString;
        readonly byte[] cancelRequestBinary;
        readonly string orderTriggerJsonString;
        readonly byte[] orderTriggerBinary;
        readonly string bookRequestJsonString;
        readonly byte[] bookRequestBinary;
        readonly string bookJsonString;
        readonly byte[] bookBinary;
        readonly byte[] orderMsgPck;
        readonly byte[] bookRequest2Bytes;
        readonly Order order;
        readonly Fill fill;
        readonly CancelledOrder cancelledOrder;
        readonly CancelRequest cancelRequest;
        readonly OrderTrigger orderTrigger;
        readonly Order2 order2;
        readonly BookDepth bookDepth;
        readonly BookRequest bookRequest;
        readonly BookRequest2 bookRequest2;
        public SerializeBenchmark()
        {
            book = new Book();
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = true, OpenQuantity = 1000, OrderId = 3434, Price = 234 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = true, OpenQuantity = 1000, OrderId = 3434, Price = 235 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = true, OpenQuantity = 1000, OrderId = 3435, Price = 236 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = true, OpenQuantity = 1000, OrderId = 3436, Price = 237 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = true, OpenQuantity = 1000, OrderId = 3437, Price = 238 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = false, OpenQuantity = 1000, OrderId = 3438, Price = 239 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = false, OpenQuantity = 1000, OrderId = 3439, Price = 240 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = false, OpenQuantity = 1000, OrderId = 3440, Price = 241 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = false, OpenQuantity = 1000, OrderId = 3441, Price = 242 });
            book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = false, OpenQuantity = 1000, OrderId = 3442, Price = 243 });
            bid = book.BidSide.ToDictionary(x => x.Key, x => x.Value.Quantity);
            ask = book.AskSide.ToDictionary(x => x.Key, x => x.Value.Quantity);

            var order = new Order() { CancelOn = 12345678, IsBuy = true, OrderId = 56789, Price = 404, OpenQuantity = 1000, OrderCondition = OrderCondition.ImmediateOrCancel, StopPrice = 9534, TotalQuantity = 7878234 };
            orderJsonString = JsonConvert.SerializeObject(order);

            orderBinarySerialized = new byte[OrderSerializer.MessageSize];
            OrderSerializer.Serialize(order, orderBinarySerialized);

            orderMsgPck = MessagePackSerializer.Serialize(new Order2 { IsBuy = true, IsTip = false, OpenQuantity = 100, OrderId = 1001, Price = 400, Quantity = 100, Sequnce = 0, StopPrice = 0 });

            fillJsonString = JsonConvert.SerializeObject(new Fill { MakerOrderId = 10001, MatchQuantity = 2000, MatchRate = 2400, TakerOrderId = 9999, Timestamp = 10303 });
            fillBinary = new byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(new Fill { MakerOrderId = 10001, MatchQuantity = 2000, MatchRate = 2400, TakerOrderId = 9999, Timestamp = 10303 }, fillBinary);

            cancelJsonString = JsonConvert.SerializeObject(new CancelledOrder { OrderId = 1201, CancelReason = CancelReason.UserRequested, RemainingQuantity = 2000, Timestamp = 234 });
            cancelBinary = new byte[CancelledOrderSerializer.MessageSize];
            CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = 1201, CancelReason = CancelReason.UserRequested, RemainingQuantity = 2000, Timestamp = 234 }, cancelBinary);

            cancelRequestJsonString = JsonConvert.SerializeObject(new CancelRequest { OrderId = 1023 });
            cancelRequestBinary = new byte[CancelRequestSerializer.MessageSize];
            CancelRequestSerializer.Serialize(new CancelRequest { OrderId = 1023 }, cancelRequestBinary);

            orderTriggerBinary = new byte[OrderTriggerSerializer.MessageSize];
            OrderTriggerSerializer.Serialize(new OrderTrigger { OrderId = 3453, Timestamp = 35345 }, orderTriggerBinary);
            orderTriggerJsonString = JsonConvert.SerializeObject(new OrderTrigger { OrderId = 3453, Timestamp = 35345 });

            bookRequestBinary = new byte[BookRequestSerializer.MessageSize];
            BookRequestSerializer.Serialize(new BookRequest { }, bookRequestBinary);
            bookRequestJsonString = JsonConvert.SerializeObject(new BookRequest { });
            bookRequest2Bytes = MessagePackSerializer.Serialize(new BookRequest2 { });

            bookDepth = new BookDepth(1234, 100, bid, ask);
            bookJsonString = JsonConvert.SerializeObject(bookDepth);
            bookBinary = new byte[OrderSerializer.MessageSize];
            BookSerializer.Serialize(bookDepth, bookBinary);

            fill = new Fill { MakerOrderId = 10001, MatchQuantity = 2000, MatchRate = 2400, TakerOrderId = 9999, Timestamp = 10303 };
            order2 = new Order2 { IsBuy = true, IsTip = false, OpenQuantity = 100, OrderId = 1001, Price = 400, Quantity = 100, Sequnce = 0, StopPrice = 0 };
            this.order = new Order { IsBuy = true, OpenQuantity = 100, OrderId = 1001, Price = 400, Sequnce = 0, TotalQuantity = 100, OrderCondition = OrderCondition.None, StopPrice = 0 };
            cancelledOrder = new CancelledOrder { OrderId = 1201, CancelReason = CancelReason.UserRequested, RemainingQuantity = 2000, Timestamp = 234 };
            cancelRequest = new CancelRequest { OrderId = 1023 };
            orderTrigger = new OrderTrigger { OrderId = 3453, Timestamp = 35345 };
            bookRequest = new BookRequest { LevelCount = 50 };
            bookRequest2 = new BookRequest2 { LevelCount = 50 };
        }

        [Benchmark]
        public void OrderJsonDeserialize()
        {
            JsonConvert.DeserializeObject<Order>(orderJsonString);
        }

        [Benchmark]
        public void OrderMsgpckDeserialize()
        {
            MessagePackSerializer.Deserialize<Order2>(orderMsgPck);
        }

        [Benchmark]
        public void OrderBinaryDeserialize()
        {
            OrderSerializer.Deserialize(orderBinarySerialized);
        }

        [Benchmark]
        public void FillJsonDeserialize()
        {
            JsonConvert.DeserializeObject<Fill>(fillJsonString);
        }

        [Benchmark]
        public void FillBinaryDeserialize()
        {
            FillSerializer.Deserialize(fillBinary);
        }

        [Benchmark]
        public void CancelJsonDeserialize()
        {
            JsonConvert.DeserializeObject<CancelledOrder>(cancelJsonString);
        }

        [Benchmark]
        public void CancelBinaryDeserialize()
        {
            CancelledOrderSerializer.Deserialize(cancelBinary);
        }

        [Benchmark]
        public void OrderTriggerJsonDeserialize()
        {
            JsonConvert.DeserializeObject<OrderTrigger>(orderTriggerJsonString);
        }

        [Benchmark]
        public void OrderTriggerBinaryDeserialize()
        {
            OrderTriggerSerializer.Deserialize(orderTriggerBinary);
        }

        [Benchmark]
        public void CancelRequestJsonDeserialize()
        {
            JsonConvert.DeserializeObject<CancelRequest>(cancelRequestJsonString);
        }

        [Benchmark]
        public void CancelRequestBinaryDeserialize()
        {
            CancelRequestSerializer.Deserialize(cancelRequestBinary);
        }

        [Benchmark]
        public void BookRequestJsonDeserialize()
        {
            JsonConvert.DeserializeObject<BookRequest>(bookRequestJsonString);
        }

        [Benchmark]
        public void BookRequestMsgPckDeserialize()
        {
            MessagePackSerializer.Deserialize<BookRequest2>(bookRequest2Bytes);
        }


        [Benchmark]
        public void BookRequestBinaryDeserialize()
        {
            BookRequestSerializer.Deserialize(bookRequestBinary);
        }

        [Benchmark]
        public void BookJsonDeserialize()
        {
            JsonConvert.DeserializeObject<BookDepth>(bookJsonString);
        }

        [Benchmark]
        public void BookBinaryDeserialize()
        {
            BookSerializer.Deserialize(bookBinary);
        }

        [Benchmark]
        public void OrderJsonSerialize()
        {
            JsonConvert.SerializeObject(order);
        }

        [Benchmark]
        public void OrderMsgPckSerialize()
        {
            MessagePackSerializer.Serialize(order2);
        }

        [Benchmark]
        public void OrderBinarySerialize()
        {
            Span<byte> bytes = stackalloc byte[OrderSerializer.MessageSize];
            OrderSerializer.Serialize(order, bytes);
        }

        [Benchmark]
        public void FillJsonSerialize()
        {
            JsonConvert.SerializeObject(fill);
        }

        [Benchmark]
        public void FillBinarySerialize()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(fill, bytes);
        }

        [Benchmark]
        public void CancelJsonSerialize()
        {
            JsonConvert.SerializeObject(cancelledOrder);
        }

        [Benchmark]
        public void CancelBinarySerialize()
        {
            Span<byte> bytes = stackalloc byte[CancelledOrderSerializer.MessageSize];
            CancelledOrderSerializer.Serialize(cancelledOrder, bytes);
        }

        [Benchmark]
        public void CancelRequestJsonSerialize()
        {
            JsonConvert.SerializeObject(cancelRequest);
        }

        [Benchmark]
        public void CancelRequestBinarySerialize()
        {
            Span<byte> bytes = stackalloc byte[CancelRequestSerializer.MessageSize];
            CancelRequestSerializer.Serialize(cancelRequest, bytes);
        }

        [Benchmark]
        public void TriggerBinarySerialize()
        {
            Span<byte> bytes = stackalloc byte[OrderTriggerSerializer.MessageSize];
            OrderTriggerSerializer.Serialize(orderTrigger, bytes);
        }

        [Benchmark]
        public void TriggerJsonSerialize()
        {
            JsonConvert.SerializeObject(orderTrigger);
        }

        [Benchmark]
        public void BookRequestBinarySerialize()
        {
            Span<byte> bytes = stackalloc byte[BookRequestSerializer.MessageSize];
            BookRequestSerializer.Serialize(bookRequest, bytes);
        }

        [Benchmark]
        public void BookRequestMsgPckSerialize()
        {
            MessagePackSerializer.Serialize(bookRequest2);
        }

        [Benchmark]
        public void BookRequestJsonSerialize()
        {
            JsonConvert.SerializeObject(bookRequest);
        }

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

        [Benchmark]
        public void BookSerializeBinarySerialize()
        {
            Span<byte> bytes = stackalloc byte[10];
            BookSerializer.Serialize(bookDepth, bytes);
        }

        [Benchmark]
        public void BookJsonSerialize()
        {
            JsonConvert.SerializeObject(bookDepth);
        }
    }
}
