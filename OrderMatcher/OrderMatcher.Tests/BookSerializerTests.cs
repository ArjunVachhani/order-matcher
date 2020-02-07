using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class BookSerializerTests
    {

        [Fact]
        public void Serialize_ThrowsExecption_IfNullPassed()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => BookSerializer.Serialize(null, 10, null, 10));
            Assert.Equal("book", ex.ParamName);
        }

        [Fact]
        public void Serialize_ThrowsExecption_IfLevelNegativePassed()
        {
            var book = new Book();
            ArgumentException ex = Assert.Throws<ArgumentException>(() => BookSerializer.Serialize(book, -1, null, 10));
            Assert.Equal("levels should be non negative", ex.Message);
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
            Assert.Equal("Invalid Message", ex.Message);
        }

        [Fact]
        public void Deserialize_ThrowsExecption_IfVersionIsNotSet()
        {
            var bytes = new byte[31];
            bytes[4] = (byte)MessageType.Book;
            Exception ex = Assert.Throws<Exception>(() => BookSerializer.Deserialize(bytes));
            Assert.Equal("version mismatch", ex.Message);
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_EmptyBook()
        {
            var book = new Book();
            var bytes = BookSerializer.Serialize(book, 10, 10, DateTime.UtcNow.Ticks);
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_OnlyBidOrder()
        {
            var book = new Book();
            book.AddOrderOpenBook(new Order { IsBuy = true, Price = 10 });
            var bytes = BookSerializer.Serialize(book, 10, 10, DateTime.UtcNow.Ticks);
        }

        [Fact]
        public void Serialize_Doesnotthrowexception_OnlyAskOrder()
        {
            var book = new Book();
            book.AddOrderOpenBook(new Order { IsBuy = false, Price = 10 });
            var bytes = BookSerializer.Serialize(book, 10, 10, DateTime.UtcNow.Ticks);
        }

        [Fact]
        public void Serialize_Doesnotthrowexception()
        {
            var book = new Book();
            book.AddOrderOpenBook(new Order { IsBuy = false, Price = 10 });
            book.AddOrderOpenBook(new Order { IsBuy = true, Price = 10 });
            var bytes = BookSerializer.Serialize(book, 10, 10, DateTime.UtcNow.Ticks);
        }

        [Fact]
        public void Deserialize_CheckCorrectBidCountAskCountForEmpty()
        {
            var book = new Book();
            var bytes = BookSerializer.Serialize(book, 10, 1010, DateTime.UtcNow.Ticks);
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(35, messageLength);
            var bookDepth = BookSerializer.Deserialize(bytes);
            Assert.Equal(1010, bookDepth.LTP);
            Assert.NotEqual(0, bookDepth.TimeStamp);
            Assert.Empty(bookDepth.Bid);
            Assert.Empty(bookDepth.Ask);
        }

        [Fact]
        public void Deserialize_CheckCorrectBidCountAskCountWithPriceLevel()
        {
            var book = new Book();
            book.AddOrderOpenBook(new Order { IsBuy = true, Price = 9,  OpenQuantity = 10 });
            book.AddOrderOpenBook(new Order { IsBuy = true, Price = 8,  OpenQuantity = 9 });
            book.AddOrderOpenBook(new Order { IsBuy = false, Price = 10,  OpenQuantity = 10 });
            var bytes = BookSerializer.Serialize(book, 9, 11000, DateTime.UtcNow.Ticks);
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(131, messageLength);
            var bookDepth = BookSerializer.Deserialize(bytes);
            Assert.Equal(11000, bookDepth.LTP);
            Assert.Equal(2, bookDepth.Bid.Count);
            Assert.Single(bookDepth.Ask);
            Assert.Equal((Price)9, bookDepth.Bid[0].Key);
            Assert.Equal((Quantity)10, bookDepth.Bid[0].Value);
            Assert.Equal((Price)8, bookDepth.Bid[1].Key);
            Assert.Equal((Quantity)9, bookDepth.Bid[1].Value);
            Assert.Equal((Price)10, bookDepth.Ask[0].Key);
            Assert.Equal((Quantity)10, bookDepth.Ask[0].Value);
        }

        [Fact]
        public void Deserialize_CheckCorrectOnlyBuy()
        {
            var book = new Book();
            book.AddOrderOpenBook(new Order { IsBuy = true, Price = 9, OpenQuantity = 10 });
            book.AddOrderOpenBook(new Order { IsBuy = true, Price = 8, OpenQuantity = 9 });
            var bytes = BookSerializer.Serialize(book, 9, 11000, DateTime.UtcNow.Ticks);
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(99, messageLength);
            var bookDepth = BookSerializer.Deserialize(bytes);
            Assert.Equal(11000, bookDepth.LTP);
            Assert.Equal(2, bookDepth.Bid.Count);
            Assert.Empty(bookDepth.Ask);
            Assert.Equal((Price)9, bookDepth.Bid[0].Key);
            Assert.Equal((Quantity)10, bookDepth.Bid[0].Value);
            Assert.Equal((Price)8, bookDepth.Bid[1].Key);
            Assert.Equal((Quantity)9, bookDepth.Bid[1].Value);
        }

        [Fact]
        public void Deserialize_CheckCorrectOnlyAsk()
        {
            var book = new Book();
            book.AddOrderOpenBook(new Order { IsBuy = false, Price = 10,  OpenQuantity = 10 });
            var bytes = BookSerializer.Serialize(book, 9, 11000, DateTime.UtcNow.Ticks);
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(67, messageLength);
            var bookDepth = BookSerializer.Deserialize(bytes);
            Assert.Equal(11000, bookDepth.LTP);
            Assert.Empty(bookDepth.Bid);
            Assert.Single(bookDepth.Ask);
            Assert.Equal((Price)10, bookDepth.Ask[0].Key);
            Assert.Equal((Quantity)10, bookDepth.Ask[0].Value);
        }

        [Fact]
        public void Deserialize_CheckCorrectBidCountAskFullBook()
        {
            var book = new Book();
            book.AddOrderOpenBook(new Order { IsBuy = true, Price = 10, OpenQuantity = 1 });
            book.AddOrderOpenBook(new Order { IsBuy = true, Price = 9,  OpenQuantity = 2 });
            book.AddOrderOpenBook(new Order { IsBuy = true, Price = 8,  OpenQuantity = 3 });
            book.AddOrderOpenBook(new Order { IsBuy = true, Price = 7, OpenQuantity = 4 });
            book.AddOrderOpenBook(new Order { IsBuy = true, Price = 6, OpenQuantity = 5 });
            book.AddOrderOpenBook(new Order { IsBuy = true, Price = 5, OpenQuantity = 6 });
            book.AddOrderOpenBook(new Order { IsBuy = true, Price = 4, OpenQuantity = 7 });
            book.AddOrderOpenBook(new Order { IsBuy = true, Price = 3, OpenQuantity = 8 });
            book.AddOrderOpenBook(new Order { IsBuy = true, Price = 2, OpenQuantity = 9 });
            book.AddOrderOpenBook(new Order { IsBuy = true, Price = 1, OpenQuantity = 10 });

            book.AddOrderOpenBook(new Order { IsBuy = false, Price = 11,  OpenQuantity = 11 });
            book.AddOrderOpenBook(new Order { IsBuy = false, Price = 12,  OpenQuantity = 12 });
            book.AddOrderOpenBook(new Order { IsBuy = false, Price = 13,  OpenQuantity = 13 });
            book.AddOrderOpenBook(new Order { IsBuy = false, Price = 14,  OpenQuantity = 14 });
            book.AddOrderOpenBook(new Order { IsBuy = false, Price = 15,  OpenQuantity = 15 });
            book.AddOrderOpenBook(new Order { IsBuy = false, Price = 16,  OpenQuantity = 16 });
            book.AddOrderOpenBook(new Order { IsBuy = false, Price = 17, OpenQuantity = 17 });
            book.AddOrderOpenBook(new Order { IsBuy = false, Price = 18,  OpenQuantity = 18 });
            book.AddOrderOpenBook(new Order { IsBuy = false, Price = 19,  OpenQuantity = 19 });
            book.AddOrderOpenBook(new Order { IsBuy = false, Price = 20,  OpenQuantity = 20 });

            var bytes = BookSerializer.Serialize(book, 5, 10, DateTime.UtcNow.Ticks);
            var messageLength = BitConverter.ToInt32(bytes, 0);
            Assert.Equal(355, messageLength);
            var bookDepth = BookSerializer.Deserialize(bytes);
            Assert.Equal(10, bookDepth.LTP);
            Assert.Equal(5, bookDepth.Bid.Count);

            Assert.Equal((Price)10, bookDepth.Bid[0].Key);
            Assert.Equal((Quantity)1, bookDepth.Bid[0].Value);
            Assert.Equal((Price)9, bookDepth.Bid[1].Key);
            Assert.Equal((Quantity)2, bookDepth.Bid[1].Value);
            Assert.Equal((Price)8, bookDepth.Bid[2].Key);
            Assert.Equal((Quantity)3, bookDepth.Bid[2].Value);
            Assert.Equal((Price)7, bookDepth.Bid[3].Key);
            Assert.Equal((Quantity)4, bookDepth.Bid[3].Value);
            Assert.Equal((Price)6, bookDepth.Bid[4].Key);
            Assert.Equal((Quantity)5, bookDepth.Bid[4].Value);


            Assert.Equal((Price)11, bookDepth.Ask[0].Key);
            Assert.Equal((Quantity)11, bookDepth.Ask[0].Value);
            Assert.Equal((Price)12, bookDepth.Ask[1].Key);
            Assert.Equal((Quantity)12, bookDepth.Ask[1].Value);
            Assert.Equal((Price)13, bookDepth.Ask[2].Key);
            Assert.Equal((Quantity)13, bookDepth.Ask[2].Value);
            Assert.Equal((Price)14, bookDepth.Ask[3].Key);
            Assert.Equal((Quantity)14, bookDepth.Ask[3].Value);
            Assert.Equal((Price)15, bookDepth.Ask[4].Key);
            Assert.Equal((Quantity)15, bookDepth.Ask[4].Value);
        }
    }
}
