using BenchmarkDotNet.Attributes;
using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System;
using System.Linq;
using System.Text.Json;

namespace OrderMatcher.Performance
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class OrderBookSerializeBenchmark
    {
        readonly BookDepth bookDepth;
        public OrderBookSerializeBenchmark()
        {
            var book = new Book();
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

            var bid = book.BidSide.ToDictionary(x => x.Key, x => x.Value.Quantity);
            var ask = book.AskSide.ToDictionary(x => x.Key, x => x.Value.Quantity);

            bookDepth = new BookDepth(1234, 100, bid, ask);
        }

        [Benchmark(Baseline = true)]
        public void BookSerializeBinarySerialize()
        {
            Span<byte> bytes = stackalloc byte[BookSerializer.GetMessageSize(bookDepth)];
            BookSerializer.Serialize(bookDepth, bytes);
        }

        //[Benchmark] //skip as System.Text.Json does not support non string as dictionary key type
        public void BookJsonSerialize()
        {
            JsonSerializer.SerializeToUtf8Bytes(bookDepth);
        }
    }
}
