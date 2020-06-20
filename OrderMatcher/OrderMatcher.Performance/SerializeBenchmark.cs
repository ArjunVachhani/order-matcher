using BenchmarkDotNet.Attributes;
using MessagePack;
using Newtonsoft.Json;
using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System;
using System.Collections;

namespace OrderMatcher.Performance
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class SerializeBenchmark
    {
        //readonly Book book;
        //readonly List<KeyValuePair<Price, Quantity>> bid;
        //readonly List<KeyValuePair<Price, Quantity>> ask;
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
        //readonly string bookJsonString;
        //readonly byte[] bookBinary;
        readonly byte[] orderMsgPck;
        readonly byte[] bookRequest2;

        readonly Fill fill;
        public SerializeBenchmark()
        {
            //book = new Book();
            //book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = true, IsTip = false, OpenQuantity = 1000, OrderId = 3434, Price = 234 });
            //book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = true, IsTip = false, OpenQuantity = 1000, OrderId = 3434, Price = 235 });
            //book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = true, IsTip = false, OpenQuantity = 1000, OrderId = 3435, Price = 236 });
            //book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = true, IsTip = false, OpenQuantity = 1000, OrderId = 3436, Price = 237 });
            //book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = true, IsTip = false, OpenQuantity = 1000, OrderId = 3437, Price = 238 });
            //book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = false, IsTip = false, OpenQuantity = 1000, OrderId = 3438, Price = 239 });
            //book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = false, IsTip = false, OpenQuantity = 1000, OrderId = 3439, Price = 240 });
            //book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = false, IsTip = false, OpenQuantity = 1000, OrderId = 3440, Price = 241 });
            //book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = false, IsTip = false, OpenQuantity = 1000, OrderId = 3441, Price = 242 });
            //book.AddOrderOpenBook(new Order { CancelOn = 1000, IsBuy = false, IsTip = false, OpenQuantity = 1000, OrderId = 3442, Price = 243 });
            //bid = book.BidSide.Select(x => new KeyValuePair<Price, Quantity>(x.Key, x.Value.Quantity)).ToList();
            //ask = book.AskSide.Select(x => new KeyValuePair<Price, Quantity>(x.Key, x.Value.Quantity)).ToList();

            //var order = new Order { CancelOn = 12345678, IsBuy = true, OrderId = 56789, Price = 404, OpenQuantity = 1000 };
            //var orderWrapper = new OrderWrapper() { Order = order, OrderCondition = OrderCondition.ImmediateOrCancel, StopPrice = 9534, TotalQuantity = 7878234 };
            //orderJsonString = JsonConvert.SerializeObject(orderWrapper);
            //orderBinarySerialized = OrderSerializer.Serialize(orderWrapper);
            //orderMsgPck = MessagePackSerializer.Serialize(new Order2 { IsBuy = true, IsTip = false, OpenQuantity = 100, OrderId = 1001, Price = 400, Quantity = 100, Sequnce = 0, StopPrice = 0 });

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
            bookRequest2 = MessagePackSerializer.Serialize(new BookRequest2 { });

            //bookJsonString = JsonConvert.SerializeObject(new BookDepth { Bid = bid, Ask = ask, LTP = 100, TimeStamp = 1234 });
            //bookBinary = BookSerializer.Serialize(book, 5, 100, 1234);

            fill = new Fill { MakerOrderId = 10001, MatchQuantity = 2000, MatchRate = 2400, TakerOrderId = 9999, Timestamp = 10303 };
        }

        //[Benchmark]
        //public void OrderJsonDeserialize()
        //{
        //    var order = JsonConvert.DeserializeObject<Order>(orderJsonString);
        //}

        //[Benchmark]
        //public void OrderMsgpckDeserialize()
        //{
        //    var order = MessagePackSerializer.Deserialize<Order2>(orderMsgPck);
        //}

        //[Benchmark]
        //public void OrderBinaryDeserialize()
        //{
        //    var order = OrderSerializer.Deserialize(orderBinarySerialized);
        //}

        //[Benchmark]
        //public void FillJsonDeserialize()
        //{
        //    var fill = JsonConvert.DeserializeObject<Fill>(fillJsonString);
        //}

        //[Benchmark]
        //public void FillBinaryDeserialize()
        //{
        //    var fill = FillSerializer.Deserialize(fillBinary);
        //}

        //[Benchmark]
        //public void CancelJsonDeserialize()
        //{
        //    var cancel = JsonConvert.DeserializeObject<CancelledOrder>(cancelJsonString);
        //}

        //[Benchmark]
        //public void CancelBinaryDeserialize()
        //{
        //    var cancel = CancelledOrderSerializer.Deserialize(cancelBinary);
        //}

        //[Benchmark]
        //public void OrderTriggerJsonDeserialize()
        //{
        //    var trigger = JsonConvert.DeserializeObject<OrderTrigger>(orderTriggerJsonString);
        //}

        //[Benchmark]
        //public void OrderTriggerBinaryDeserialize()
        //{
        //    var trigger = OrderTriggerSerializer.Deserialize(orderTriggerBinary);
        //}

        //[Benchmark]
        //public void CancelRequestJsonDeserialize()
        //{
        //    var cancelrequest = JsonConvert.DeserializeObject<CancelRequest>(cancelRequestJsonString);
        //}

        //[Benchmark]
        //public void CancelRequestBinaryDeserialize()
        //{
        //    var cancelRequest = CancelRequestSerializer.Deserialize(cancelRequestBinary);
        //}

        //[Benchmark]
        //public void BookRequestJsonDeserialize()
        //{
        //    var bookrequest = JsonConvert.DeserializeObject<BookRequest>(bookRequestJsonString);
        //}

        //[Benchmark]
        //public void BookRequestMsgPckDeserialize()
        //{
        //    var bookrequest = MessagePackSerializer.Deserialize<BookRequest2>(bookRequest2);
        //}


        //[Benchmark]
        //public void BookRequestBinaryDeserialize()
        //{
        //    var bookrequest = BookRequestSerializer.Deserialize(bookRequestBinary);
        //}

        ////[Benchmark]
        ////public void BookJsonDeserialize()
        ////{
        ////    var bookDepth = JsonConvert.DeserializeObject<BookDepth>(bookJsonString);
        ////}

        ////[Benchmark]
        ////public void BookBinaryDeserialize()
        ////{
        ////    var bookDepth = BookSerializer.Deserialize(bookBinary);
        ////}

        ////[Benchmark]
        ////public void OrderJsonSerialize()
        ////{
        ////    var order = new Order { IsBuy = true, IsTip = false, OpenQuantity = 100, OrderId = 1001, Price = 400, Sequnce = 0 };
        ////    var orderWrapper = new OrderWrapper() { TotalQuantity = 100, OrderCondition = OrderCondition.None, StopPrice = 0 };
        ////    var orderJsonString = JsonConvert.SerializeObject(orderWrapper);
        ////}

        ////[Benchmark]
        ////public void OrderMsgPckSerialize()
        ////{
        ////    var msgPck = MessagePackSerializer.Serialize(new Order2 { IsBuy = true, IsTip = false, OpenQuantity = 100, OrderId = 1001, Price = 400, Quantity = 100, Sequnce = 0, StopPrice = 0 });
        ////}

        ////[Benchmark]
        ////public void OrderBinarySerialize()
        ////{
        ////    var order = new Order { IsBuy = true, IsTip = false, OpenQuantity = 100, OrderId = 1001, Price = 400, Sequnce = 0 };
        ////    var orderWrapper = new OrderWrapper() { TotalQuantity = 100, OrderCondition = OrderCondition.None, StopPrice = 0 };
        ////    var bytes = OrderSerializer.Serialize(orderWrapper);
        ////}

        //[Benchmark]
        //public void FillJsonSerialize()
        //{
        //    var fill = JsonConvert.SerializeObject(new Fill { MakerOrderId = 10001, MatchQuantity = 2000, MatchRate = 2400, TakerOrderId = 9999, Timestamp = 10303 });
        //}

        [Benchmark]
        public void FillBinarySerialize()
        {
            Span<byte> bytes = stackalloc byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(fill, bytes);
        }

        //[Benchmark]
        //public void CancelJsonSerialize()
        //{
        //    var cancel = JsonConvert.SerializeObject(new CancelledOrder { OrderId = 1201, CancelReason = CancelReason.UserRequested, RemainingQuantity = 2000, Timestamp = 234 });
        //}

        //[Benchmark]
        //public void CancelBinarySerialize()
        //{
        //    Span<byte> bytes = stackalloc byte[CancelledOrderSerializer.MessageSize];
        //    CancelledOrderSerializer.Serialize(new CancelledOrder { OrderId = 1201, CancelReason = CancelReason.UserRequested, RemainingQuantity = 2000, Timestamp = 234 }, bytes);
        //}

        //[Benchmark]
        //public void CancelRequestJsonSerialize()
        //{
        //    var bytes = JsonConvert.SerializeObject(new CancelRequest { OrderId = 1023 });
        //}

        //[Benchmark]
        //public void CancelRequestBinarySerialize()
        //{
        //    Span<byte> bytes = stackalloc byte[CancelRequestSerializer.MessageSize];
        //    CancelRequestSerializer.Serialize(new CancelRequest { OrderId = 1023 }, bytes);
        //}

        //[Benchmark]
        //public void TriggerBinarySerialize()
        //{
        //    Span<byte> bytes = stackalloc byte[OrderTriggerSerializer.MessageSize];
        //    OrderTriggerSerializer.Serialize(new OrderTrigger { OrderId = 3453, Timestamp = 35345 }, bytes);
        //}

        //[Benchmark]
        //public void TriggerJsonSerialize()
        //{
        //    var msg = JsonConvert.SerializeObject(new OrderTrigger { OrderId = 3453, Timestamp = 35345 });
        //}

        //[Benchmark]
        //public void BookRequestBinarySerialize()
        //{
        //    Span<byte> bytes = stackalloc byte[BookRequestSerializer.MessageSize];
        //    BookRequestSerializer.Serialize(new BookRequest { }, bytes);
        //}

        //[Benchmark]
        //public void BookRequestMsgPckSerialize()
        //{
        //    var msg = MessagePackSerializer.Serialize(new BookRequest2 { });
        //}

        //[Benchmark]
        //public void BookRequestJsonSerialize()
        //{
        //    var msg = JsonConvert.SerializeObject(new BookRequest { });
        //}

        //[Benchmark]
        //public void Slice()
        //{
        //    Span<byte> bytes = stackalloc byte[10];
        //    bytes.Slice(3);
        //}

        //[Benchmark]
        //public void SliceWithLength()
        //{
        //    Span<byte> bytes = stackalloc byte[10];
        //    bytes.Slice(3,4);            
        //}

        //[Benchmark]
        //public void BookSerializeBinarySerialize()
        //{
        //    var msg = BookSerializer.Serialize(book, 50, 100, 1234);
        //}

        //[Benchmark]
        //public void BookJsonSerialize()
        //{
        //    var msg = JsonConvert.SerializeObject(new BookDepth { Bid = bid, Ask = ask, LTP = 100, TimeStamp = 1234 });
        //}
    }
}
