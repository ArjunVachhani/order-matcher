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
    public class BookRequestSerializeBenchmark
    {
        readonly BookRequest bookRequest;
        readonly BookRequest2 bookRequest2;

        public BookRequestSerializeBenchmark()
        {
            bookRequest = new BookRequest { LevelCount = 50 };
            bookRequest2 = new BookRequest2 { LevelCount = 50 };
        }

        [Benchmark(Baseline = true)]
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
            JsonSerializer.SerializeToUtf8Bytes(bookRequest);
        }
    }
}
