using OrderMatcher.Types;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OrderMatcher.Tests
{
    public class BookTests
    {
        [Fact]
        public void GetBestBuyOrderToMatch_Retuns_Best_Order_Rate_For_Buy()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(1000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = true, OrderId = 2, UserId = 2, Price = 14, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(14, book.BestBidPrice);
            Assert.Equal(1000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 14, 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000, 1000 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = true, OrderId = 3, UserId = 3, Price = 9, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order3);

            Assert.Equal(14, book.BestBidPrice);
            Assert.Equal(1000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 14, 10, 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000, 1000, 1000 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order = book.GetBestBuyOrderToMatch(true);
            Assert.Equal(order, order2);
        }

        [Fact]
        public void GetBestBuyOrderToMatch_Retuns_Best_Order_Rate_For_Sell()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(10, book.BestAskPrice);
            Assert.Equal(1000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, Price = 7, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(7, book.BestAskPrice);
            Assert.Equal(1000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000, 1000 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = false, OrderId = 3, UserId = 3, Price = 9, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order3);

            Assert.Equal(7, book.BestAskPrice);
            Assert.Equal(1000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000, 1000, 1000 }, book.AskSide.Select(x => x.Quantity).ToList());
            Order order = book.GetBestBuyOrderToMatch(false);
            Assert.Equal(order, order2);
        }

        [Fact]
        public void GetBestBuyOrderToMatch_Retuns_Null_Is_No_Order_For_Buy()
        {
            Book book = new Book();
            Order order = book.GetBestBuyOrderToMatch(true);
            Assert.Null(order);

            AssertHelper.SequentiallyEqual(new Price[] { }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { }, book.BidSide.Select(x => x.Quantity).ToList());
            Assert.Null(book.BestBidPrice);
            Assert.Null(book.BestBidQuantity);
        }

        [Fact]
        public void GetBestBuyOrderToMatch_Retuns_Null_Is_No_Order_For_Sell()
        {
            Book book = new Book();
            Order order = book.GetBestBuyOrderToMatch(false);
            Assert.Null(order);

            AssertHelper.SequentiallyEqual(new Price[] { }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { }, book.AskSide.Select(x => x.Quantity).ToList());
            Assert.Null(book.BestAskPrice);
            Assert.Null(book.BestAskQuantity);
        }

        [Fact]
        public void GetBestBuyOrderToMatch_Retuns_First_Order_If_Rate_Is_Same_For_Buy()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order1);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000 }, book.BidSide.Select(x => x.Quantity).ToList());
            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(1000, book.BestBidQuantity);

            Order order2 = new Order { IsBuy = true, OrderId = 2, UserId = 2, Price = 14, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order2);
            AssertHelper.SequentiallyEqual(new Price[] { 14, 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000, 1000 }, book.BidSide.Select(x => x.Quantity).ToList());
            Assert.Equal(14, book.BestBidPrice);
            Assert.Equal(1000, book.BestBidQuantity);

            Order order3 = new Order { IsBuy = true, OrderId = 3, UserId = 3, Price = 14, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order3);
            AssertHelper.SequentiallyEqual(new Price[] { 14, 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 2000, 1000 }, book.BidSide.Select(x => x.Quantity).ToList());
            Assert.Equal(14, book.BestBidPrice);
            Assert.Equal(2000, book.BestBidQuantity);

            Order order4 = new Order { IsBuy = true, OrderId = 4, UserId = 4, Price = 9, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order4);

            Assert.Equal(14, book.BestBidPrice);
            Assert.Equal(2000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 14, 10, 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 2000, 1000, 1000 }, book.BidSide.Select(x => x.Quantity).ToList());
            Order order = book.GetBestBuyOrderToMatch(true);
            Assert.Equal(order, order2);
        }

        [Fact]
        public void GetBestBuyOrderToMatch_Retuns_First_Order_If_Rate_Is_Same_For_Sell()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order1);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000 }, book.AskSide.Select(x => x.Quantity).ToList());
            Assert.Equal(10, book.BestAskPrice);
            Assert.Equal(1000, book.BestAskQuantity);

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, Price = 7, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order2);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000, 1000 }, book.AskSide.Select(x => x.Quantity).ToList());
            Assert.Equal(7, book.BestAskPrice);
            Assert.Equal(1000, book.BestAskQuantity);

            Order order3 = new Order { IsBuy = false, OrderId = 3, UserId = 3, Price = 9, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order3);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000, 1000, 1000 }, book.AskSide.Select(x => x.Quantity).ToList());
            Assert.Equal(7, book.BestAskPrice);
            Assert.Equal(1000, book.BestAskQuantity);

            Order order4 = new Order { IsBuy = false, OrderId = 4, UserId = 4, Price = 7, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order4);

            Assert.Equal(7, book.BestAskPrice);
            Assert.Equal(2000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 2000, 1000, 1000 }, book.AskSide.Select(x => x.Quantity).ToList());
            Order order = book.GetBestBuyOrderToMatch(false);
            Assert.Equal(order, order2);
        }

        [Fact]
        public void AddStopOrder_Adds_Order_In_Stop_Order_Book_Based_On_Price_Priotity_For_Buy()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, Price = 10, StopPrice = 9 };
            book.AddStopOrder(order1);
            Assert.Equal(9, book.BestStopBidPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.StopBidSide.Select(x => x.Price).ToList());

            Order order2 = new Order { IsBuy = true, OrderId = 2, UserId = 2, Price = 10, StopPrice = 10 };
            book.AddStopOrder(order2);
            Assert.Equal(9, book.BestStopBidPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 9, 10 }, book.StopBidSide.Select(x => x.Price).ToList());

            Order order3 = new Order { IsBuy = true, OrderId = 3, UserId = 3, Price = 10, StopPrice = 7 };
            book.AddStopOrder(order3);
            Assert.Equal(7, book.BestStopBidPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.StopBidSide.Select(x => x.Price).ToList());

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);

            Assert.Equal(7, book.BestStopBidPrice);
            List<Order> orders = book.StopBidSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order3, order1, order2 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);
        }

        [Fact]
        public void AddStopOrder_Adds_Order_In_Stop_Order_Book_Based_On_Time_For_Same_Stop_Price_Level_Priotity_For_Buy()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 10, StopPrice = 9 };
            book.AddStopOrder(order1);
            Assert.Equal(9, book.BestStopBidPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.StopBidSide.Select(x => x.Price).ToList());

            Order order2 = new Order { IsBuy = true, OrderId = 2, UserId = 2, Price = 10, OpenQuantity = 10, StopPrice = 10 };
            book.AddStopOrder(order2);
            Assert.Equal(9, book.BestStopBidPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 9, 10 }, book.StopBidSide.Select(x => x.Price).ToList());

            Order order3 = new Order { IsBuy = true, OrderId = 3, UserId = 3, Price = 10, OpenQuantity = 10, StopPrice = 7 };
            book.AddStopOrder(order3);
            Assert.Equal(7, book.BestStopBidPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.StopBidSide.Select(x => x.Price).ToList());

            Order order4 = new Order { IsBuy = true, OrderId = 4, UserId = 4, Price = 10, OpenQuantity = 10, StopPrice = 9 };
            book.AddStopOrder(order4);
            Assert.Equal(7, book.BestStopBidPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.StopBidSide.Select(x => x.Price).ToList());

            Order order5 = new Order { IsBuy = true, OrderId = 5, UserId = 5, Price = 10, OpenQuantity = 10, StopPrice = 10 };
            book.AddStopOrder(order5);
            Assert.Equal(7, book.BestStopBidPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.StopBidSide.Select(x => x.Price).ToList());

            Order order6 = new Order { IsBuy = true, OrderId = 6, UserId = 6, Price = 10, OpenQuantity = 10, StopPrice = 7 };
            book.AddStopOrder(order6);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.StopBidSide.Select(x => x.Price).ToList());

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);
            Assert.Equal((ulong)4, order4.Sequence);
            Assert.Equal((ulong)5, order5.Sequence);
            Assert.Equal((ulong)6, order6.Sequence);

            Assert.Equal(7, book.BestStopBidPrice);
            List<Order> orders = book.StopBidSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order3, order6, order1, order4, order2, order5 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);
        }

        [Fact]
        public void AddStopOrder_Adds_Order_In_Stop_Order_Book_Based_On_Price_Priotity_For_Sell()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 10, StopPrice = 9 };
            book.AddStopOrder(order1);
            Assert.Equal(9, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.StopAskSide.Select(x => x.Price).ToList());

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, Price = 10, OpenQuantity = 10, StopPrice = 10 };
            book.AddStopOrder(order2);
            Assert.Equal(10, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9 }, book.StopAskSide.Select(x => x.Price).ToList());

            Order order3 = new Order { IsBuy = false, OrderId = 3, UserId = 3, Price = 10, OpenQuantity = 10, StopPrice = 7 };
            book.AddStopOrder(order3);
            Assert.Equal(10, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9, 7 }, book.StopAskSide.Select(x => x.Price).ToList());

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);

            Assert.Equal(10, book.BestStopAskPrice);
            List<Order> orders = book.StopAskSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order2, order1, order3 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);
        }

        [Fact]
        public void AddStopOrder_Adds_Order_In_Stop_Order_Book_Based_On_Time_For_Same_Stop_Price_Level_Priotity_For_Sell()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 10, StopPrice = 9 };
            book.AddStopOrder(order1);
            Assert.Equal(9, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.StopAskSide.Select(x => x.Price).ToList());

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, Price = 10, OpenQuantity = 10, StopPrice = 10 };
            book.AddStopOrder(order2);
            Assert.Equal(10, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9 }, book.StopAskSide.Select(x => x.Price).ToList());

            Order order3 = new Order { IsBuy = false, OrderId = 3, UserId = 3, Price = 10, OpenQuantity = 10, StopPrice = 7 };
            book.AddStopOrder(order3);
            Assert.Equal(10, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9, 7 }, book.StopAskSide.Select(x => x.Price).ToList());

            Order order4 = new Order { IsBuy = false, OrderId = 4, UserId = 4, Price = 10, OpenQuantity = 10, StopPrice = 9 };
            book.AddStopOrder(order4);
            Assert.Equal(10, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9, 7 }, book.StopAskSide.Select(x => x.Price).ToList());

            Order order5 = new Order { IsBuy = false, OrderId = 5, UserId = 5, Price = 10, OpenQuantity = 10, StopPrice = 10 };
            book.AddStopOrder(order5);
            Assert.Equal(10, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9, 7 }, book.StopAskSide.Select(x => x.Price).ToList());

            Order order6 = new Order { IsBuy = false, OrderId = 6, UserId = 6, Price = 10, OpenQuantity = 10, StopPrice = 7 };
            book.AddStopOrder(order6);
            Assert.Equal(10, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9, 7 }, book.StopAskSide.Select(x => x.Price).ToList());

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);
            Assert.Equal((ulong)4, order4.Sequence);
            Assert.Equal((ulong)5, order5.Sequence);
            Assert.Equal((ulong)6, order6.Sequence);

            Assert.Equal(10, book.BestStopAskPrice);
            List<Order> orders = book.StopAskSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order2, order5, order1, order4, order3, order6 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);
        }

        [Fact]
        public void RemoveOrder_Removes_Buy_Order_From_Open_Book_For()
        {
            Book book = new Book();

            Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order1);
            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(1000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, Price = 10, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order2);
            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(2000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 2000 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order() { IsBuy = true, OrderId = 3, UserId = 3, Price = 10, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order3);
            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(3000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 3000 }, book.BidSide.Select(x => x.Quantity).ToList());

            book.RemoveOrder(order2);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(2000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 2000 }, book.BidSide.Select(x => x.Quantity).ToList());

            List<Order> expectedResult = new List<Order> { order1, order3 };
            AssertHelper.SequentiallyEqual(expectedResult, book.BidSide.SelectMany(x => x));

            Order order = book.GetBestBuyOrderToMatch(true);
            Assert.Equal(order1, order);
        }

        [Fact]
        public void RemoveOrder_Removes_Buy_Order_Removes_PriceLevel_From_Open_Book_For_Buy()
        {
            Book book = new Book();

            Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order1);
            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(1000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, Price = 11, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order2);
            Assert.Equal(11, book.BestBidPrice);
            Assert.Equal(1000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 11, 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000, 1000 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order() { IsBuy = true, OrderId = 3, UserId = 3, Price = 12, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order3);
            Assert.Equal(12, book.BestBidPrice);
            Assert.Equal(1000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 12, 11, 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000, 1000, 1000 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order4 = new Order() { IsBuy = true, OrderId = 4, UserId = 4, Price = 12, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order4);

            Assert.Equal(12, book.BestBidPrice);
            Assert.Equal(2000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 12, 11, 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 2000, 1000, 1000 }, book.BidSide.Select(x => x.Quantity).ToList());

            book.RemoveOrder(order2);
            Assert.Equal(12, book.BestBidPrice);
            Assert.Equal(2000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 12, 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 2000, 1000 }, book.BidSide.Select(x => x.Quantity).ToList());

            List<Order> expectedResult = new List<Order> { order3, order4, order1 };
            AssertHelper.SequentiallyEqual(expectedResult, book.BidSide.SelectMany(x => x));

            Assert.Equal(2, book.BidSide.Count());

            Order order = book.GetBestBuyOrderToMatch(true);
            Assert.Equal(order3, order);
        }

        [Fact]
        public void RemoveOrder_Removes_Buy_Order_From_Stop_Book()
        {
            Book book = new Book();

            Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 100000, StopPrice = 10 };
            book.AddStopOrder(order1);
            Assert.Equal(10, book.BestStopBidPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.StopBidSide.Select(x => x.Price).ToList());

            Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, Price = 10, OpenQuantity = 100000, StopPrice = 10 };
            book.AddStopOrder(order2);
            Assert.Equal(10, book.BestStopBidPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.StopBidSide.Select(x => x.Price).ToList());

            Order order3 = new Order() { IsBuy = true, OrderId = 3, UserId = 3, Price = 10, OpenQuantity = 100000, StopPrice = 10 };
            book.AddStopOrder(order3);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.StopBidSide.Select(x => x.Price).ToList());

            Order order4 = new Order() { IsBuy = true, OrderId = 4, UserId = 4, Price = 10, OpenQuantity = 100000, StopPrice = 11 };
            book.AddStopOrder(order4);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 11 }, book.StopBidSide.Select(x => x.Price).ToList());

            Assert.Equal(10, book.BestStopBidPrice);

            book.RemoveOrder(order2);

            Assert.Equal(10, book.BestStopBidPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 11 }, book.StopBidSide.Select(x => x.Price).ToList());

            List<Order> expectedResult = new List<Order> { order1, order3, order4 };
            AssertHelper.SequentiallyEqual(expectedResult, book.StopBidSide.SelectMany(x => x));

            book.RemoveOrder(order1);
            book.RemoveOrder(order3);

            Assert.Equal(11, book.BestStopBidPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 11 }, book.StopBidSide.Select(x => x.Price).ToList());

            expectedResult = new List<Order> { order4 };
            AssertHelper.SequentiallyEqual(expectedResult, book.StopBidSide.SelectMany(x => x));

            book.RemoveOrder(order4);
            Assert.Null(book.BestStopBidPrice);
            Assert.Empty(book.StopBidSide);
        }

        [Fact]
        public void RemoveOrder_Removes_Buy_Order_Removes_PriceLevel_From_Stop_Book()
        {
            Book book = new Book();

            Order order3 = new Order() { IsBuy = true, OrderId = 3, UserId = 3, Price = 12, OpenQuantity = 100000, StopPrice = 12 };
            book.AddStopOrder(order3);
            Assert.Equal(12, book.BestStopBidPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 12 }, book.StopBidSide.Select(x => x.Price).ToList());

            Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 100000, StopPrice = 10 };
            book.AddStopOrder(order1);
            Assert.Equal(10, book.BestStopBidPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 12 }, book.StopBidSide.Select(x => x.Price).ToList());

            Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, Price = 11, OpenQuantity = 100000, StopPrice = 11 };
            book.AddStopOrder(order2);
            Assert.Equal(10, book.BestStopBidPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 11, 12 }, book.StopBidSide.Select(x => x.Price).ToList());

            Order order4 = new Order() { IsBuy = true, OrderId = 4, UserId = 4, Price = 12, OpenQuantity = 100000, StopPrice = 12 };
            book.AddStopOrder(order4);


            Assert.Equal(10, book.BestStopBidPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 11, 12 }, book.StopBidSide.Select(x => x.Price).ToList());

            book.RemoveOrder(order2);

            Assert.Equal(10, book.BestStopBidPrice);

            List<Order> expectedResult = new List<Order> { order1, order3, order4 };

            AssertHelper.SequentiallyEqual(expectedResult, book.StopBidSide.SelectMany(x => x));

            Assert.Equal(2, book.StopBidSide.Count());
        }

        [Fact]
        public void RemoveOrder_Removes_Sell_Order_From_Open_Book_For()
        {
            Book book = new Book();

            Order order1 = new Order() { IsBuy = false, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(10, book.BestAskPrice);
            Assert.Equal(1000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order() { IsBuy = false, OrderId = 2, UserId = 2, Price = 10, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(10, book.BestAskPrice);
            Assert.Equal(2000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 2000 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order() { IsBuy = false, OrderId = 3, UserId = 3, Price = 10, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order3);

            Assert.Equal(10, book.BestAskPrice);
            Assert.Equal(3000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 3000 }, book.AskSide.Select(x => x.Quantity).ToList());

            book.RemoveOrder(order2);

            Assert.Equal(10, book.BestAskPrice);
            Assert.Equal(2000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 2000 }, book.AskSide.Select(x => x.Quantity).ToList());

            List<Order> expectedResult = new List<Order> { order1, order3 };
            AssertHelper.SequentiallyEqual(expectedResult, book.AskSide.SelectMany(x => x));

            Order order = book.GetBestBuyOrderToMatch(false);
            Assert.Equal(order1, order);
        }

        [Fact]
        public void RemoveOrder_Removes_Sell_Order_Removes_PriceLevel_From_Open_Book_For()
        {
            Book book = new Book();

            Order order1 = new Order() { IsBuy = false, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(10, book.BestAskPrice);
            Assert.Equal(1000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order() { IsBuy = false, OrderId = 2, UserId = 2, Price = 11, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(10, book.BestAskPrice);
            Assert.Equal(1000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 11 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000, 1000 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order() { IsBuy = false, OrderId = 3, UserId = 3, Price = 12, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order3);

            Assert.Equal(10, book.BestAskPrice);
            Assert.Equal(1000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 11, 12 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000, 1000, 1000 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order4 = new Order() { IsBuy = false, OrderId = 4, UserId = 4, Price = 12, OpenQuantity = 1000 };
            book.AddOrderOpenBook(order4);

            Assert.Equal(10, book.BestAskPrice);
            Assert.Equal(1000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 11, 12 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000, 1000, 2000 }, book.AskSide.Select(x => x.Quantity).ToList());

            book.RemoveOrder(order2);

            Assert.Equal(10, book.BestAskPrice);
            Assert.Equal(1000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 12 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 1000, 2000 }, book.AskSide.Select(x => x.Quantity).ToList());

            List<Order> expectedResult = new List<Order> { order1, order3, order4 };
            AssertHelper.SequentiallyEqual(expectedResult, book.AskSide.SelectMany(x => x));

            Assert.Equal(2, book.AskSide.Count());

            Order order = book.GetBestBuyOrderToMatch(false);
            Assert.Equal(order1, order);
        }

        [Fact]
        public void RemoveOrder_Removes_Sell_Order_From_Stop_Book()
        {
            Book book = new Book();

            Order order1 = new Order() { IsBuy = false, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 100000, StopPrice = 10 };
            book.AddStopOrder(order1);
            Assert.Equal(10, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.StopAskSide.Select(x => x.Price).ToList());

            Order order2 = new Order() { IsBuy = false, OrderId = 2, UserId = 2, Price = 10, OpenQuantity = 100000, StopPrice = 10 };
            book.AddStopOrder(order2);
            Assert.Equal(10, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.StopAskSide.Select(x => x.Price).ToList());

            Order order3 = new Order() { IsBuy = false, OrderId = 3, UserId = 3, Price = 10, OpenQuantity = 100000, StopPrice = 10 };
            book.AddStopOrder(order3);

            Assert.Equal(10, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.StopAskSide.Select(x => x.Price).ToList());

            book.RemoveOrder(order2);

            Assert.Equal(10, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.StopAskSide.Select(x => x.Price).ToList());

            List<Order> expectedResult = new List<Order> { order1, order3 };
            AssertHelper.SequentiallyEqual(expectedResult, book.StopAskSide.SelectMany(x => x));
        }

        [Fact]
        public void RemoveOrder_Removes_Sell_Order_Removes_PriceLevel_From_Stop_Book()
        {
            Book book = new Book();

            Order order1 = new Order() { IsBuy = false, OrderId = 3, UserId = 3, Price = 12, OpenQuantity = 100000, StopPrice = 12 };
            book.AddStopOrder(order1);
            Assert.Equal(12, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 12 }, book.StopAskSide.Select(x => x.Price).ToList());

            Order order2 = new Order() { IsBuy = false, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 100000, StopPrice = 10 };
            book.AddStopOrder(order2);
            Assert.Equal(12, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 12, 10 }, book.StopAskSide.Select(x => x.Price).ToList());

            Order order3 = new Order() { IsBuy = false, OrderId = 2, UserId = 2, Price = 11, OpenQuantity = 100000, StopPrice = 11 };
            book.AddStopOrder(order3);
            Assert.Equal(12, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 12, 11, 10 }, book.StopAskSide.Select(x => x.Price).ToList());

            Order order4 = new Order() { IsBuy = false, OrderId = 4, UserId = 4, Price = 12, OpenQuantity = 100000, StopPrice = 12 };
            book.AddStopOrder(order4);
            Assert.Equal(12, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 12, 11, 10 }, book.StopAskSide.Select(x => x.Price).ToList());

            book.RemoveOrder(order3);

            Assert.Equal(12, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 12, 10 }, book.StopAskSide.Select(x => x.Price).ToList());

            List<Order> expectedResult = new List<Order> { order1, order4, order2 };

            AssertHelper.SequentiallyEqual(expectedResult, book.StopAskSide.SelectMany(x => x));

            Assert.Equal(2, book.StopAskSide.Count());

            book.RemoveOrder(order1);
            book.RemoveOrder(order4);
            Assert.Equal(10, book.BestStopAskPrice);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.StopAskSide.Select(x => x.Price).ToList());
            expectedResult = new List<Order> { order2 };
            AssertHelper.SequentiallyEqual(expectedResult, book.StopAskSide.SelectMany(x => x));
            Assert.Single(book.StopAskSide);

            book.RemoveOrder(order2);
            Assert.Null(book.BestStopAskPrice);
            Assert.Empty(book.StopAskSide);
        }

        [Fact]
        public void AddOrderOpenBook_Adds_Order_In_Open_Order_Book_Based_On_Price_Priotity_For_Buy()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = true, OrderId = 2, UserId = 2, OpenQuantity = 10, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = true, OrderId = 3, UserId = 3, OpenQuantity = 10, Price = 7 };
            book.AddOrderOpenBook(order3);

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9, 7 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10, 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);

            List<Order> orders = book.BidSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order2, order1, order3 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);
        }

        [Fact]
        public void AddOrderOpenBook_Adds_Order_In_Open_Order_Book_Based_On_Time_For_Same_Open_Price_Level_Priotity_For_Buy()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = true, OrderId = 2, UserId = 2, OpenQuantity = 10, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = true, OrderId = 3, UserId = 3, OpenQuantity = 10, Price = 7 };
            book.AddOrderOpenBook(order3);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9, 7 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10, 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order4 = new Order { IsBuy = true, OrderId = 4, UserId = 4, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order4);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9, 7 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 20, 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order5 = new Order { IsBuy = true, OrderId = 5, UserId = 5, OpenQuantity = 10, Price = 10 };
            book.AddOrderOpenBook(order5);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(20, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9, 7 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 20, 20, 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order6 = new Order { IsBuy = true, OrderId = 6, UserId = 6, OpenQuantity = 10, Price = 7 };
            book.AddOrderOpenBook(order6);

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);
            Assert.Equal((ulong)4, order4.Sequence);
            Assert.Equal((ulong)5, order5.Sequence);
            Assert.Equal((ulong)6, order6.Sequence);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(20, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9, 7 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 20, 20, 20 }, book.BidSide.Select(x => x.Quantity).ToList());

            List<Order> orders = book.BidSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order2, order5, order1, order4, order3, order6 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);
        }

        [Fact]
        public void AddOrderOpenBook_Adds_Order_In_Open_Order_Book_Based_On_Price_Priotity_For_Sell()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, OpenQuantity = 10, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = false, OrderId = 3, UserId = 3, OpenQuantity = 10, Price = 7 };
            book.AddOrderOpenBook(order3);

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);

            Assert.Equal(7, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10, 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            List<Order> orders = book.AskSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order3, order1, order2 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);
        }

        [Fact]
        public void AddOrderOpenBook_Adds_Order_In_Open_Order_Book_Based_On_Time_For_Same_Open_Price_Level_Priotity_For_Sell()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, OpenQuantity = 10, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = false, OrderId = 3, UserId = 3, OpenQuantity = 10, Price = 7 };
            book.AddOrderOpenBook(order3);

            Assert.Equal(7, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10, 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order4 = new Order { IsBuy = false, OrderId = 4, UserId = 4, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order4);

            Assert.Equal(7, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 20, 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order5 = new Order { IsBuy = false, OrderId = 5, UserId = 5, OpenQuantity = 10, Price = 10 };
            book.AddOrderOpenBook(order5);

            Assert.Equal(7, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 20, 20 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order6 = new Order { IsBuy = false, OrderId = 6, UserId = 6, OpenQuantity = 10, Price = 7 };
            book.AddOrderOpenBook(order6);

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);
            Assert.Equal((ulong)4, order4.Sequence);
            Assert.Equal((ulong)5, order5.Sequence);
            Assert.Equal((ulong)6, order6.Sequence);

            Assert.Equal(7, book.BestAskPrice);
            Assert.Equal(20, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 20, 20, 20 }, book.AskSide.Select(x => x.Quantity).ToList());

            List<Order> orders = book.AskSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order3, order6, order1, order4, order2, order5 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);
        }

        [Fact]
        public void FillOrder_Reduce_Open_Quantity_For_Buy()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 100000, Sequence = 1, Price = 10 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(100000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 100000 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = true, OrderId = 2, UserId = 2, OpenQuantity = 100000, Sequence = 2, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(200000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 200000 }, book.BidSide.Select(x => x.Quantity).ToList());

            bool fillResult1 = book.FillOrder(order1, 900);
            Assert.False(fillResult1);
            Assert.Equal(99100, order1.OpenQuantity);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(199100, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 199100 }, book.BidSide.Select(x => x.Quantity).ToList());

            bool fillResult2 = book.FillOrder(order1, 100);
            Assert.False(fillResult2);
            Assert.Equal(99000, order1.OpenQuantity);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(199000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 199000 }, book.BidSide.Select(x => x.Quantity).ToList());

            Assert.Equal(100000, order2.OpenQuantity);
        }

        [Fact]
        public void FillOrder_Removes_Level_If_No_Order_In_Level_For_Buy()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 100000, Sequence = 1, Price = 10 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(100000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 100000 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = true, OrderId = 2, UserId = 2, OpenQuantity = 100000, Sequence = 2, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(200000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 200000 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = true, OrderId = 3, UserId = 3, OpenQuantity = 100000, Sequence = 3, Price = 9 };
            book.AddOrderOpenBook(order3);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(200000, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 200000, 100000 }, book.BidSide.Select(x => x.Quantity).ToList());

            bool fillResult1 = book.FillOrder(order1, 90000);
            Assert.False(fillResult1);
            Assert.Equal(10000, order1.OpenQuantity);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(110000, book.BestBidQuantity);

            bool fillResult2 = book.FillOrder(order1, 10000);
            Assert.True(fillResult2);
            Assert.Equal(0, order1.OpenQuantity);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(100000, book.BestBidQuantity);

            bool fillResult3 = book.FillOrder(order2, 100000);
            Assert.True(fillResult3);
            Assert.Equal(0, order2.OpenQuantity);

            Assert.Equal(9, book.BestBidPrice);
            Assert.Equal(100000, book.BestBidQuantity);

            int buyLevelCount = book.BidSide.Count();
            Assert.Equal(1, buyLevelCount);
            Assert.Equal(order3, book.GetBestBuyOrderToMatch(true));
        }

        [Fact]
        public void FillOrder_Reduce_Open_Quantity_For_Sell()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, OpenQuantity = 100000, Sequence = 1, Price = 10 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(10, book.BestAskPrice);
            Assert.Equal(100000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 100000 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, OpenQuantity = 100000, Sequence = 2, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(10, book.BestAskPrice);
            Assert.Equal(200000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 200000 }, book.AskSide.Select(x => x.Quantity).ToList());

            bool fillResult1 = book.FillOrder(order1, 900);
            Assert.False(fillResult1);
            Assert.Equal(99100, order1.OpenQuantity);

            Assert.Equal(10, book.BestAskPrice);
            Assert.Equal(199100, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 199100 }, book.AskSide.Select(x => x.Quantity).ToList());

            bool fillResult2 = book.FillOrder(order1, 100);
            Assert.False(fillResult2);
            Assert.Equal(99000, order1.OpenQuantity);

            Assert.Equal(10, book.BestAskPrice);
            Assert.Equal(199000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 199000 }, book.AskSide.Select(x => x.Quantity).ToList());

            Assert.Equal(100000, order2.OpenQuantity);
        }

        [Fact]
        public void FillOrder_Removes_Level_If_No_Order_In_Level_For_Sell()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, OpenQuantity = 100000, Sequence = 1, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(100000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 100000 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, OpenQuantity = 100000, Sequence = 2, Price = 9 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(200000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 200000 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = false, OrderId = 3, UserId = 3, OpenQuantity = 100000, Sequence = 3, Price = 10 };
            book.AddOrderOpenBook(order3);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(200000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 200000, 100000 }, book.AskSide.Select(x => x.Quantity).ToList());

            bool fillResult1 = book.FillOrder(order1, 90000);
            Assert.False(fillResult1);
            Assert.Equal(10000, order1.OpenQuantity);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(110000, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 110000, 100000 }, book.AskSide.Select(x => x.Quantity).ToList());

            bool fillResult2 = book.FillOrder(order1, 10000);
            Assert.True(fillResult2);
            Assert.Equal(0, order1.OpenQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 100000, 100000 }, book.AskSide.Select(x => x.Quantity).ToList());

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(100000, book.BestAskQuantity);

            bool fillResult3 = book.FillOrder(order2, 100000);
            Assert.True(fillResult3);
            Assert.Equal(0, order2.OpenQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 100000 }, book.AskSide.Select(x => x.Quantity).ToList());

            Assert.Equal(10, book.BestAskPrice);
            Assert.Equal(100000, book.BestAskQuantity);

            int askLevelCount = book.AskSide.Count();
            Assert.Equal(1, askLevelCount);
            Assert.Equal(order3, book.GetBestBuyOrderToMatch(false));
        }

        [Fact]
        public void RemoveStopAsks_Removes_PriceLevel_From_Stop_Aks()
        {
            Book book = new Book();

            Order order1 = new Order() { IsBuy = false, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 1000, StopPrice = 9 };
            book.AddStopOrder(order1);

            Assert.Equal(9, book.BestStopAskPrice);

            Order order2 = new Order() { IsBuy = false, OrderId = 2, UserId = 2, Price = 10, OpenQuantity = 1000, StopPrice = 9 };
            book.AddStopOrder(order2);

            Assert.Equal(9, book.BestStopAskPrice);

            Order order3 = new Order() { IsBuy = false, OrderId = 3, UserId = 3, Price = 10, OpenQuantity = 1000, StopPrice = 8 };
            book.AddStopOrder(order3);

            Assert.Equal(9, book.BestStopAskPrice);

            Order order4 = new Order() { IsBuy = false, OrderId = 4, UserId = 4, Price = 10, OpenQuantity = 1000, StopPrice = 7 };
            book.AddStopOrder(order4);

            Assert.Equal(9, book.BestStopAskPrice);

            Order order5 = new Order() { IsBuy = false, OrderId = 5, UserId = 5, Price = 10, OpenQuantity = 1000, StopPrice = 7 };
            book.AddStopOrder(order5);

            Assert.Equal(9, book.BestStopAskPrice);

            Order order6 = new Order() { IsBuy = false, OrderId = 6, UserId = 6, Price = 10, OpenQuantity = 1000, StopPrice = 6 };
            book.AddStopOrder(order6);

            Assert.Equal(9, book.BestStopAskPrice);

            List<PriceLevel> priceLevels = book.RemoveStopAsks(new Price(8));
            Assert.Equal(2, priceLevels.Count());

            List<Order> expectedResult = new List<Order> { order1, order2, order3 };
            AssertHelper.SequentiallyEqual(expectedResult, priceLevels.SelectMany(x => x.ToList()));

            Assert.Equal(7, book.BestStopAskPrice);
            Assert.Equal(new Price(7), book.StopAskSide.First().Price);

            List<Order> expectedResult2 = new List<Order> { order4, order5, order6 };
            AssertHelper.SequentiallyEqual(expectedResult2, book.StopAskSide.SelectMany(x => x));

            Assert.Equal(2, book.StopAskSide.Count());
        }

        [Fact]
        public void RemoveStopAsks_Returns_Null_If_No_Price_Level_In_Limit()
        {
            Book book = new Book();

            Order order4 = new Order() { IsBuy = false, OrderId = 4, UserId = 4, Price = 10, OpenQuantity = 1000, StopPrice = 7 };
            book.AddStopOrder(order4);

            Assert.Equal(7, book.BestStopAskPrice);

            Order order5 = new Order() { IsBuy = false, OrderId = 5, UserId = 5, Price = 10, OpenQuantity = 1000, StopPrice = 7 };
            book.AddStopOrder(order5);

            Assert.Equal(7, book.BestStopAskPrice);

            Order order6 = new Order() { IsBuy = false, OrderId = 6, UserId = 6, Price = 10, OpenQuantity = 1000, StopPrice = 6 };
            book.AddStopOrder(order6);

            Assert.Equal(7, book.BestStopAskPrice);

            List<PriceLevel> priceLevels = book.RemoveStopAsks(new Price(8));
            Assert.Null(priceLevels);

            Assert.Equal(7, book.BestStopAskPrice);

            Assert.Equal(new Price(7), book.StopAskSide.First().Price);

            List<Order> expectedResult2 = new List<Order> { order4, order5, order6 };
            AssertHelper.SequentiallyEqual(expectedResult2, book.StopAskSide.SelectMany(x => x));

            Assert.Equal(2, book.StopAskSide.Count());
        }

        [Fact]
        public void RemoveStopAsks_Returns_Null_If_No_Price_Level_Exists()
        {
            Book book = new Book();

            Order order4 = new Order() { IsBuy = false, OrderId = 4, UserId = 4, Price = 10, OpenQuantity = 1000, StopPrice = 7 };
            book.AddStopOrder(order4);

            Assert.Equal(7, book.BestStopAskPrice);

            Order order5 = new Order() { IsBuy = false, OrderId = 5, UserId = 5, Price = 10, OpenQuantity = 1000, StopPrice = 7 };
            book.AddStopOrder(order5);

            Assert.Equal(7, book.BestStopAskPrice);

            Order order6 = new Order() { IsBuy = false, OrderId = 6, UserId = 6, Price = 10, OpenQuantity = 1000, StopPrice = 6 };
            book.AddStopOrder(order6);

            Assert.Equal(7, book.BestStopAskPrice);

            List<PriceLevel> priceLevels = book.RemoveStopAsks(new Price(8));
            Assert.Null(priceLevels);

            Assert.Equal(7, book.BestStopAskPrice);

            Assert.Equal(new Price(7), book.StopAskSide.First().Price);

            List<Order> expectedResult2 = new List<Order> { order4, order5, order6 };
            AssertHelper.SequentiallyEqual(expectedResult2, book.StopAskSide.SelectMany(x => x));

            Assert.Equal(2, book.StopAskSide.Count());
        }

        [Fact]
        public void RemoveStopBids_Removes_PriceLevel_From_Stop_Bids()
        {
            Book book = new Book();

            Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 10, OpenQuantity = 1000, StopPrice = 10 };
            book.AddStopOrder(order1);

            Assert.Equal(10, book.BestStopBidPrice);

            Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, Price = 10, OpenQuantity = 1000, StopPrice = 10};
            book.AddStopOrder(order2);

            Assert.Equal(10, book.BestStopBidPrice);

            Order order3 = new Order() { IsBuy = true, OrderId = 3, UserId = 3, Price = 10, OpenQuantity = 1000, StopPrice = 11};
            book.AddStopOrder(order3);

            Assert.Equal(10, book.BestStopBidPrice);

            Order order4 = new Order() { IsBuy = true, OrderId = 4, UserId = 4, Price = 10, OpenQuantity = 1000, StopPrice = 12 };
            book.AddStopOrder(order4);

            Assert.Equal(10, book.BestStopBidPrice);

            Order order5 = new Order() { IsBuy = true, OrderId = 5, UserId = 5, Price = 10, OpenQuantity = 1000, StopPrice = 13};
            book.AddStopOrder(order5);

            Assert.Equal(10, book.BestStopBidPrice);

            Order order6 = new Order() { IsBuy = true, OrderId = 6, UserId = 6, Price = 10, OpenQuantity = 1000, StopPrice = 13 };
            book.AddStopOrder(order6);

            Assert.Equal(10, book.BestStopBidPrice);

            List<PriceLevel> priceLevels = book.RemoveStopBids(new Price(11));
            Assert.Equal(2, priceLevels.Count());

            Assert.Equal(12, book.BestStopBidPrice);

            List<Order> expectedResult = new List<Order> { order1, order2, order3 };
            AssertHelper.SequentiallyEqual(expectedResult, priceLevels.SelectMany(x => x.ToList()));

            Assert.Equal(new Price(12), book.StopBidSide.First().Price);

            List<Order> expectedResult2 = new List<Order> { order4, order5, order6 };
            AssertHelper.SequentiallyEqual(expectedResult2, book.StopBidSide.SelectMany(x => x));

            Assert.Equal(2, book.StopBidSide.Count());
        }

        [Fact]
        public void RemoveStopBids_Returns_Null_If_No_Price_Level_In_Limit()
        {
            Book book = new Book();

            Order order4 = new Order() { IsBuy = true, OrderId = 4, UserId = 4, Price = 10, OpenQuantity = 1000, StopPrice = 12 };
            book.AddStopOrder(order4);

            Assert.Equal(12, book.BestStopBidPrice);

            Order order5 = new Order() { IsBuy = true, OrderId = 5, UserId = 5, Price = 10, OpenQuantity = 1000, StopPrice = 13 };
            book.AddStopOrder(order5);

            Assert.Equal(12, book.BestStopBidPrice);

            Order order6 = new Order() { IsBuy = true, OrderId = 6, UserId = 6, Price = 10, OpenQuantity = 1000, StopPrice = 13 };
            book.AddStopOrder(order6);

            Assert.Equal(12, book.BestStopBidPrice);

            List<PriceLevel> priceLevels = book.RemoveStopBids(new Price(11));
            Assert.Null(priceLevels);

            Assert.Equal(12, book.BestStopBidPrice);

            Assert.Equal(new Price(12), book.StopBidSide.First().Price);

            List<Order> expectedResult2 = new List<Order> { order4, order5, order6 };
            AssertHelper.SequentiallyEqual(expectedResult2, book.StopBidSide.SelectMany(x => x));

            Assert.Equal(2, book.StopBidSide.Count());
        }

        [Fact]
        public void RemoveStopBids_Returns_Null_If_No_Price_Level_Exists()
        {
            Book book = new Book();

            List<PriceLevel> priceLevels = book.RemoveStopBids(new Price(11));
            Assert.Null(priceLevels);

            Assert.Null(book.BestStopBidPrice);

            Assert.Empty(book.StopBidSide);

            Assert.Null(book.BestStopAskPrice);

            Assert.Empty(book.StopBidSide);
        }

        [Fact]
        public void CheckCanFillOrder_Returns_True_If_Enough_Qty_Available_Buy_Side_For_Limit_Sell()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = true, OrderId = 2, UserId = 2, OpenQuantity = 10, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = true, OrderId = 3, UserId = 3, OpenQuantity = 10, Price = 7 };
            book.AddOrderOpenBook(order3);

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9, 7 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10, 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            List<Order> orders = book.BidSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order2, order1, order3 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);

            var result = book.CheckCanFillOrder(false, 30, 7);
            Assert.True(result);


            var result2 = book.CheckCanFillOrder(false, 29, 7);
            Assert.True(result2);


            var result3 = book.CheckCanFillOrder(false, 20, 9);
            Assert.True(result3);


            var result4 = book.CheckCanFillOrder(false, 19, 9);
            Assert.True(result4);


            var result5 = book.CheckCanFillOrder(false, 10, 10);
            Assert.True(result5);


            var result6 = book.CheckCanFillOrder(false, 9, 10);
            Assert.True(result6);
        }

        [Fact]
        public void CheckCanFillOrder_Returns_True_If_Enough_Qty_Available_Buy_Side_For_Market_Sell()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = true, OrderId = 2, UserId = 2, OpenQuantity = 10, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = true, OrderId = 3, UserId = 3, OpenQuantity = 10, Price = 7 };
            book.AddOrderOpenBook(order3);

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9, 7 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10, 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            List<Order> orders = book.BidSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order2, order1, order3 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);

            var result = book.CheckCanFillOrder(false, 30, 0);
            Assert.True(result);


            var result2 = book.CheckCanFillOrder(false, 29, 0);
            Assert.True(result2);
        }

        [Fact]
        public void CheckCanFillOrder_Returns_False_If_Not_Enough_Qty_Available_Buy_Side_For_Limit_Sell()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = true, OrderId = 2, UserId = 2, OpenQuantity = 10, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = true, OrderId = 3, UserId = 3, OpenQuantity = 10, Price = 7 };
            book.AddOrderOpenBook(order3);

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9, 7 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10, 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            List<Order> orders = book.BidSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order2, order1, order3 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);

            var result = book.CheckCanFillOrder(false, 31, 7);
            Assert.False(result);

            var result2 = book.CheckCanFillOrder(false, 21, 9);
            Assert.False(result2);

            var result3 = book.CheckCanFillOrder(false, 11, 10);
            Assert.False(result3);
        }

        [Fact]
        public void CheckCanFillOrder_Returns_False_If_Not_Enough_Qty_Available_Buy_Side_For_Market_Sell()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = true, OrderId = 2, UserId = 2, OpenQuantity = 10, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = true, OrderId = 3, UserId = 3, OpenQuantity = 10, Price = 7 };
            book.AddOrderOpenBook(order3);

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9, 7 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10, 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            List<Order> orders = book.BidSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order2, order1, order3 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);

            var result = book.CheckCanFillOrder(false, 31, 0);
            Assert.False(result);
        }

        [Fact]
        public void CheckCanFillOrder_Returns_True_If_Enough_Qty_Available_Sell_Side_For_Limit_Buy()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, OpenQuantity = 10, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = false, OrderId = 3, UserId = 3, OpenQuantity = 10, Price = 7 };
            book.AddOrderOpenBook(order3);

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);

            Assert.Equal(7, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10, 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            List<Order> orders = book.AskSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order3, order1, order2 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);

            var result = book.CheckCanFillOrder(true, 10, 7);
            Assert.True(result);


            var result2 = book.CheckCanFillOrder(true, 9, 7);
            Assert.True(result2);


            var result3 = book.CheckCanFillOrder(true, 20, 9);
            Assert.True(result3);


            var result4 = book.CheckCanFillOrder(true, 19, 9);
            Assert.True(result4);


            var result5 = book.CheckCanFillOrder(true, 30, 10);
            Assert.True(result5);


            var result6 = book.CheckCanFillOrder(true, 29, 10);
            Assert.True(result6);
        }

        [Fact]
        public void CheckCanFillOrder_Returns_True_If_Enough_Qty_Available_Sell_Side_For_Market_Buy()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, OpenQuantity = 10, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = false, OrderId = 3, UserId = 3, OpenQuantity = 10, Price = 7 };
            book.AddOrderOpenBook(order3);

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);

            Assert.Equal(7, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10, 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            List<Order> orders = book.AskSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order3, order1, order2 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);

            var result = book.CheckCanFillOrder(true, 30, 0);
            Assert.True(result);


            var result2 = book.CheckCanFillOrder(true, 29, 0);
            Assert.True(result2);
        }

        [Fact]
        public void CheckCanFillOrder_Returns_False_If_Not_Enough_Qty_Available_Sell_Side_For_Limit_Buy()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, OpenQuantity = 10, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = false, OrderId = 3, UserId = 3, OpenQuantity = 10, Price = 7 };
            book.AddOrderOpenBook(order3);

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);

            Assert.Equal(7, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10, 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            List<Order> orders = book.AskSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order3, order1, order2 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);

            var result = book.CheckCanFillOrder(true, 11, 7);
            Assert.False(result);

            var result2 = book.CheckCanFillOrder(true, 21, 9);
            Assert.False(result2);

            var result3 = book.CheckCanFillOrder(true, 31, 10);
            Assert.False(result3);
        }

        [Fact]
        public void CheckCanFillOrder_Returns_False_If_Not_Enough_Qty_Available_Sell_Side_For_Market_Buy()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, OpenQuantity = 10, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = false, OrderId = 3, UserId = 3, OpenQuantity = 10, Price = 7 };
            book.AddOrderOpenBook(order3);

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);

            Assert.Equal(7, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10, 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            List<Order> orders = book.AskSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order3, order1, order2 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);

            var result = book.CheckCanFillOrder(true, 31, 0);
            Assert.False(result);
        }

        [Fact]
        public void CheckCanFillMarketOrderAmount_For_Enough_Qty_Available_Buy_Side_NoOrders()
        {
            Book book = new Book();

            var result = book.CheckCanFillMarketOrderAmount(false, 0);
            Assert.True(result);

            var result2 = book.CheckCanFillMarketOrderAmount(false, 1);
            Assert.False(result2);

            var result3 = book.CheckCanFillMarketOrderAmount(false, 2);
            Assert.False(result3);
        }

        [Fact]
        public void CheckCanFillMarketOrderAmount_For_Enough_Qty_Available_Buy_Side_One_Order()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            var result = book.CheckCanFillMarketOrderAmount(false, 90);
            Assert.True(result);


            var result2 = book.CheckCanFillMarketOrderAmount(false, 89);
            Assert.True(result2);


            var result3 = book.CheckCanFillMarketOrderAmount(false, 91);
            Assert.False(result3);
        }

        [Fact]
        public void CheckCanFillMarketOrderAmount_For_Enough_Qty_Available_Buy_Side()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = true, OrderId = 2, UserId = 2, OpenQuantity = 10, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = true, OrderId = 3, UserId = 3, OpenQuantity = 10, Price = 7 };
            book.AddOrderOpenBook(order3);

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);
            AssertHelper.SequentiallyEqual(new Price[] { 10, 9, 7 }, book.BidSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10, 10 }, book.BidSide.Select(x => x.Quantity).ToList());

            Assert.Equal(10, book.BestBidPrice);
            Assert.Equal(10, book.BestBidQuantity);
            List<Order> orders = book.BidSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order2, order1, order3 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);

            var result = book.CheckCanFillMarketOrderAmount(false, 260);
            Assert.True(result);


            var result2 = book.CheckCanFillMarketOrderAmount(false, 259);
            Assert.True(result2);


            var result3 = book.CheckCanFillMarketOrderAmount(false, 261);
            Assert.False(result3);
        }

        [Fact]
        public void CheckCanFillMarketOrderAmount_For_Not_Enough_Qty_Available_Buy_Side_NoOrders()
        {
            Book book = new Book();

            var result = book.CheckCanFillMarketOrderAmount(true, 0);
            Assert.True(result);

            var result2 = book.CheckCanFillMarketOrderAmount(true, 1);
            Assert.False(result2);

            var result3 = book.CheckCanFillMarketOrderAmount(true, 2);
            Assert.False(result3);
        }

        [Fact]
        public void CheckCanFillMarketOrderAmount_For_Not_Enough_Qty_Available_Buy_Side_OneOrder()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            var result = book.CheckCanFillMarketOrderAmount(true, 90);
            Assert.True(result);

            var result2 = book.CheckCanFillMarketOrderAmount(true, 89);
            Assert.True(result2);

            var result3 = book.CheckCanFillMarketOrderAmount(true, 91);
            Assert.False(result3);
        }

        [Fact]
        public void CheckCanFillMarketOrderAmount_For_Not_Enough_Qty_Available_Buy_Side()
        {
            Book book = new Book();

            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, OpenQuantity = 10, Price = 9 };
            book.AddOrderOpenBook(order1);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, OpenQuantity = 10, Price = 10 };
            book.AddOrderOpenBook(order2);

            Assert.Equal(9, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            AssertHelper.SequentiallyEqual(new Price[] { 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Order order3 = new Order { IsBuy = false, OrderId = 3, UserId = 3, OpenQuantity = 10, Price = 7 };
            book.AddOrderOpenBook(order3);

            Assert.Equal((ulong)1, order1.Sequence);
            Assert.Equal((ulong)2, order2.Sequence);
            Assert.Equal((ulong)3, order3.Sequence);
            AssertHelper.SequentiallyEqual(new Price[] { 7, 9, 10 }, book.AskSide.Select(x => x.Price).ToList());
            AssertHelper.SequentiallyEqual(new Quantity[] { 10, 10, 10 }, book.AskSide.Select(x => x.Quantity).ToList());

            Assert.Equal(7, book.BestAskPrice);
            Assert.Equal(10, book.BestAskQuantity);
            List<Order> orders = book.AskSide.SelectMany(x => x).ToList();
            List<Order> expectedResult = new List<Order> { order3, order1, order2 };
            AssertHelper.SequentiallyEqual(expectedResult, orders);

            var result = book.CheckCanFillMarketOrderAmount(true, 260);
            Assert.True(result);

            var result2 = book.CheckCanFillMarketOrderAmount(true, 259);
            Assert.True(result2);

            var result3 = book.CheckCanFillMarketOrderAmount(true, 261);
            Assert.False(result3);
        }
    }
}
