using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System;
using System.Collections.Generic;
using Xunit;

namespace OrderMatcher.Tests
{
    public class BookSerializerTests
    {

        [Fact]
        public void Serialize_ThrowsExecption_IfNullPassed()
        {
            var bytes = new byte[31];
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => BookSerializer.Serialize(null, bytes));
            Assert.Equal("book", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsException_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => BookSerializer.Deserialize(null));
            Assert.Equal("bytes", ex.ParamName);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsGreaterThan35Bytes()
        {
            var bytes = new byte[30];
            Exception ex = Assert.Throws<Exception>(() => BookSerializer.Deserialize(bytes));
            Assert.Equal("Book Message must be greater than of Size : 31", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfMessageIsNothaveValidType()
        {
            var bytes = new byte[31];
            Exception ex = Assert.Throws<Exception>(() => BookSerializer.Deserialize(bytes));
            Assert.Equal(Types.Constant.INVALID_MESSAGE, ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[31];
            bytes[4] = (byte)MessageType.Book;
            Exception ex = Assert.Throws<Exception>(() => BookSerializer.Deserialize(bytes));
            Assert.Equal(Types.Constant.INVALID_VERSION, ex.Message);
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_EmptyBook()
        {
            var bid = new List<KeyValuePair<Price, Quantity>>() { };
            var ask = new List<KeyValuePair<Price, Quantity>>() { };
            var book = new BookDepth(3, 10, bid, ask);
            Span<byte> bytes = stackalloc byte[31];
            BookSerializer.Serialize(book, bytes);
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_OnlyBidOrder()
        {
            var bid = new List<KeyValuePair<Price, Quantity>>() { new KeyValuePair<Price, Quantity>(10, 20) };
            var ask = new List<KeyValuePair<Price, Quantity>>() { };
            var book = new BookDepth(10, 10, bid, ask);
            Span<byte> bytes = stackalloc byte[63];
            BookSerializer.Serialize(book, bytes);
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_OnlyAskOrder()
        {
            var ask = new List<KeyValuePair<Price, Quantity>>() { new KeyValuePair<Price, Quantity>(10, 20) };
            var bid = new List<KeyValuePair<Price, Quantity>>() { };
            var book = new BookDepth(10, 10, bid, ask);
            Span<byte> bytes = stackalloc byte[63];
            BookSerializer.Serialize(book, bytes);
        }

        [Fact]
        public void Serialize_Doesnotthrowexception()
        {
            var bid = new List<KeyValuePair<Price, Quantity>>() { new KeyValuePair<Price, Quantity>(10, 20) };
            var ask = new List<KeyValuePair<Price, Quantity>>() { new KeyValuePair<Price, Quantity>(10, 20) };
            var book = new BookDepth(10, 10, bid, ask);
            Span<byte> bytes = stackalloc byte[95];
            BookSerializer.Serialize(book, bytes);
        }

        [Fact]
        public void Deserialize_CheckCorrectBidCountAskCountForEmpty()
        {
            var bid = new List<KeyValuePair<Price, Quantity>>() { };
            var ask = new List<KeyValuePair<Price, Quantity>>() { };
            var book = new BookDepth(3, 1010, bid, ask); ;
            Span<byte> bytes = stackalloc byte[31];
            BookSerializer.Serialize(book, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(31, messageLength);
            var bookDepth = BookSerializer.Deserialize(bytes);
            Assert.Equal(1010, bookDepth.LTP);
            Assert.Equal(3, bookDepth.TimeStamp);
            Assert.Empty(bookDepth.Bid);
            Assert.Empty(bookDepth.Ask);
        }

        [Fact]
        public void Deserialize_CheckCorrectBidCountAskCountWithPriceLevel()
        {
            var bid = new List<KeyValuePair<Price, Quantity>>() { new KeyValuePair<Price, Quantity>(9, 10), new KeyValuePair<Price, Quantity>(8, 9) };
            var ask = new List<KeyValuePair<Price, Quantity>>() { new KeyValuePair<Price, Quantity>(10, 10) };
            var book = new BookDepth(3, 11000, bid, ask);
            Span<byte> bytes = stackalloc byte[127];
            BookSerializer.Serialize(book, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(127, messageLength);
            var bookDepth = BookSerializer.Deserialize(bytes);
            Assert.Equal(11000, bookDepth.LTP);
            Assert.Equal(2, bookDepth.Bid.Count);
            Assert.Single(bookDepth.Ask);
            Assert.Equal(9, bookDepth.Bid[0].Key);
            Assert.Equal(10, bookDepth.Bid[0].Value);
            Assert.Equal(8, bookDepth.Bid[1].Key);
            Assert.Equal(9, bookDepth.Bid[1].Value);
            Assert.Equal(10, bookDepth.Ask[0].Key);
            Assert.Equal(10, bookDepth.Ask[0].Value);
        }

        [Fact]
        public void Deserialize_CheckCorrectOnlyBuy()
        {
            var bid = new List<KeyValuePair<Price, Quantity>>() { new KeyValuePair<Price, Quantity>(9, 10), new KeyValuePair<Price, Quantity>(8, 9) };
            var ask = new List<KeyValuePair<Price, Quantity>>() { };
            var book = new BookDepth(3, 11000, bid, ask);
            Span<byte> bytes = stackalloc byte[95];
            BookSerializer.Serialize(book, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(95, messageLength);
            var bookDepth = BookSerializer.Deserialize(bytes);
            Assert.Equal(11000, bookDepth.LTP);
            Assert.Equal(2, bookDepth.Bid.Count);
            Assert.Empty(bookDepth.Ask);
            Assert.Equal(9, bookDepth.Bid[0].Key);
            Assert.Equal(10, bookDepth.Bid[0].Value);
            Assert.Equal(8, bookDepth.Bid[1].Key);
            Assert.Equal(9, bookDepth.Bid[1].Value);
        }

        [Fact]
        public void Deserialize_CheckCorrectOnlyAsk()
        {
            var bid = new List<KeyValuePair<Price, Quantity>>() { };
            var ask = new List<KeyValuePair<Price, Quantity>>() { new KeyValuePair<Price, Quantity>(10, 10) };
            var book = new BookDepth(3, 11000, bid, ask);
            Span<byte> bytes = stackalloc byte[63];
            BookSerializer.Serialize(book, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));
            Assert.Equal(63, messageLength);
            var bookDepth = BookSerializer.Deserialize(bytes);
            Assert.Equal(11000, bookDepth.LTP);
            Assert.Empty(bookDepth.Bid);
            Assert.Single(bookDepth.Ask);
            Assert.Equal(10, bookDepth.Ask[0].Key);
            Assert.Equal(10, bookDepth.Ask[0].Value);
        }

        [Fact]
        public void Deserialize_CheckCorrectBidCountAskFullBook()
        {
            var bid = new List<KeyValuePair<Price, Quantity>>() {
                new KeyValuePair<Price, Quantity>(10,1),
                new KeyValuePair<Price, Quantity>(9,2),
                new KeyValuePair<Price, Quantity>(8,3),
                new KeyValuePair<Price, Quantity>(7,4),
                new KeyValuePair<Price, Quantity>(6,5)
            };
            var ask = new List<KeyValuePair<Price, Quantity>>() {
                new KeyValuePair<Price, Quantity>(11, 11),
                new KeyValuePair<Price, Quantity>(12,12),
                new KeyValuePair<Price, Quantity>(13,13),
                new KeyValuePair<Price, Quantity>(14,14),
                new KeyValuePair<Price, Quantity>(15,15)
            };
            var book = new BookDepth(3, 10, bid, ask);
            Span<byte> bytes = stackalloc byte[351];
            BookSerializer.Serialize(book, bytes);
            var messageLength = BitConverter.ToInt32(bytes.Slice(0));

            Assert.Equal(351, messageLength);
            var bookDepth = BookSerializer.Deserialize(bytes);
            Assert.Equal(10, bookDepth.LTP);
            Assert.Equal(5, bookDepth.Bid.Count);

            Assert.Equal(10, bookDepth.Bid[0].Key);
            Assert.Equal(1, bookDepth.Bid[0].Value);
            Assert.Equal(9, bookDepth.Bid[1].Key);
            Assert.Equal(2, bookDepth.Bid[1].Value);
            Assert.Equal(8, bookDepth.Bid[2].Key);
            Assert.Equal(3, bookDepth.Bid[2].Value);
            Assert.Equal(7, bookDepth.Bid[3].Key);
            Assert.Equal(4, bookDepth.Bid[3].Value);
            Assert.Equal(6, bookDepth.Bid[4].Key);
            Assert.Equal(5, bookDepth.Bid[4].Value);


            Assert.Equal(11, bookDepth.Ask[0].Key);
            Assert.Equal(11, bookDepth.Ask[0].Value);
            Assert.Equal(12, bookDepth.Ask[1].Key);
            Assert.Equal(12, bookDepth.Ask[1].Value);
            Assert.Equal(13, bookDepth.Ask[2].Key);
            Assert.Equal(13, bookDepth.Ask[2].Value);
            Assert.Equal(14, bookDepth.Ask[3].Key);
            Assert.Equal(14, bookDepth.Ask[3].Value);
            Assert.Equal(15, bookDepth.Ask[4].Key);
            Assert.Equal(15, bookDepth.Ask[4].Value);
        }
    }
}
