using System.Linq;

namespace OrderMatcher.Performance;

[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class OrderBookDeserializeBenchmark
{
    readonly byte[] bookJsonBytes;
    readonly byte[] bookBinary;
    public OrderBookDeserializeBenchmark()
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

        var bid = book.BidSide.ToDictionary(x => x.Price, x => x.Quantity);
        var ask = book.AskSide.ToDictionary(x => x.Price, x => x.Quantity);

        var bookDepth = new BookDepth(1234, 100, bid, ask);

        bookJsonBytes = JsonSerializer.SerializeToUtf8Bytes(bookDepth);
        bookBinary = new byte[BookSerializer.GetMessageSize(bookDepth)];
        BookSerializer.Serialize(bookDepth, bookBinary);
    }

    //[Benchmark] //skip as System.Text.Json does not support non string as dictionary key type
    public void BookJsonDeserialize()
    {
        JsonSerializer.Deserialize<BookDepth>(bookJsonBytes);
    }

    [Benchmark(Baseline = true)]
    public void BookBinaryDeserialize()
    {
        BookSerializer.Deserialize(bookBinary);
    }
}
