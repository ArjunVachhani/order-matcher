using BenchmarkDotNet.Attributes;
using MessagePack;
using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System.Text.Json;

namespace OrderMatcher.Performance
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class BookRequestDeserializeBenchmark
    {
        readonly byte[] bookRequestJsonBytes;
        readonly byte[] bookRequestBinary;
        readonly byte[] bookRequest2Bytes;

        public BookRequestDeserializeBenchmark()
        {
            bookRequestBinary = new byte[BookRequestSerializer.MessageSize];
            BookRequestSerializer.Serialize(new BookRequest { }, bookRequestBinary);
            bookRequestJsonBytes = JsonSerializer.SerializeToUtf8Bytes(new BookRequest { });
            bookRequest2Bytes = MessagePackSerializer.Serialize(new BookRequest2 { });
        }

        [Benchmark]
        public void BookRequestJsonDeserialize()
        {
            JsonSerializer.Deserialize<BookRequest>(bookRequestJsonBytes);
        }

        [Benchmark]
        public void BookRequestMsgPckDeserialize()
        {
            MessagePackSerializer.Deserialize<BookRequest2>(bookRequest2Bytes);
        }

        [Benchmark(Baseline = true)]
        public void BookRequestBinaryDeserialize()
        {
            BookRequestSerializer.Deserialize(bookRequestBinary);
        }
    }
}
