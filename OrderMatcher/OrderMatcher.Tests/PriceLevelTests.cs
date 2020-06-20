using OrderMatcher.Types;
using System;
using Xunit;

namespace OrderMatcher.Tests
{
    public class PriceLevelTests
    {
        [Fact]
        public void AddOrder_AddsOrder()
        {
            PriceLevel priceLevel = new PriceLevel(100);
            Order order1 = new Order() { IsBuy = true, OrderId = 1, Price = 100, Sequnce = 1 };
            priceLevel.AddOrder(order1);

            Assert.Equal(1, priceLevel.OrderCount);

            Order order2 = new Order() { IsBuy = true, OrderId = 2, Price = 100, Sequnce = 2 };
            priceLevel.AddOrder(order2);

            Assert.Equal(2, priceLevel.OrderCount);
            Assert.Contains(order1, priceLevel);
            Assert.Contains(order2, priceLevel);
        }

        [Fact]
        public void RemoveOrder_RemovesOrder()
        {
            PriceLevel priceLevel = new PriceLevel(100);
            Order order1 = new Order() { IsBuy = true, OrderId = 1, Price = 100, Sequnce = 1 };
            priceLevel.AddOrder(order1);

            Assert.Equal(1, priceLevel.OrderCount);

            Order order2 = new Order() { IsBuy = true, OrderId = 2, Price = 100, Sequnce = 2 };
            priceLevel.AddOrder(order2);

            Assert.Equal(2, priceLevel.OrderCount);

            Order order3 = new Order() { IsBuy = true, OrderId = 3, Price = 100, Sequnce = 3 };
            priceLevel.AddOrder(order3);

            Assert.Equal(3, priceLevel.OrderCount);

            Order order4 = new Order() { IsBuy = true, OrderId = 4, Price = 100, Sequnce = 4 };
            priceLevel.AddOrder(order4);

            Assert.Equal(4, priceLevel.OrderCount);

            priceLevel.RemoveOrder(order3);

            Assert.Equal(3, priceLevel.OrderCount);
            Assert.Contains(order1, priceLevel);
            Assert.Contains(order2, priceLevel);
            Assert.Contains(order4, priceLevel);
        }

        [Fact]
        public void Fill_RemovesOrder_IfOpenQuantityIs0()
        {
            PriceLevel priceLevel = new PriceLevel(100);
            Order order1 = new Order() { IsBuy = true, OrderId = 1, Price = 100, OpenQuantity = 1000, Sequnce = 1 };
            priceLevel.AddOrder(order1);

            Assert.Equal(1, priceLevel.OrderCount);

            Order order2 = new Order() { IsBuy = true, OrderId = 2, Price = 100, OpenQuantity = 1000, Sequnce = 2 };
            priceLevel.AddOrder(order2);

            Assert.Equal(2, priceLevel.OrderCount);

            priceLevel.Fill(order1, 1000);

            Assert.Equal(1, priceLevel.OrderCount);
            Assert.Contains(order2, priceLevel);
        }

        [Fact]
        public void Fill_DoesNotRemoveOrder_IfOpenQuantityIsNot0()
        {
            PriceLevel priceLevel = new PriceLevel(100);
            Order order1 = new Order() { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 100, Sequnce = 1 };
            priceLevel.AddOrder(order1);

            Assert.Equal(1, priceLevel.OrderCount);

            Order order2 = new Order() { IsBuy = true, OrderId = 2, OpenQuantity = 1000, Price = 100, Sequnce = 2 };
            priceLevel.AddOrder(order2);

            Assert.Equal(2, priceLevel.OrderCount);

            priceLevel.Fill(order1, 900);

            Assert.Equal(2, priceLevel.OrderCount);
            Assert.Contains(order1, priceLevel);
            Assert.Contains(order2, priceLevel);
            Assert.True(100 == order1.OpenQuantity, "Quantity should be 100");
        }

        [Fact]
        public void Fill_ThrowsException_IfOpenQuantityIsLessThanFillQuantity()
        {
            PriceLevel priceLevel = new PriceLevel(100);
            Order order1 = new Order() { IsBuy = true, OrderId = 1, Price = 100, Sequnce = 1 };
            priceLevel.AddOrder(order1);

            Assert.Equal(1, priceLevel.OrderCount);

            Order order2 = new Order() { IsBuy = true, OrderId = 2, Price = 100, Sequnce = 2 };
            priceLevel.AddOrder(order2);

            Assert.Equal(2, priceLevel.OrderCount);

            Exception ex = Assert.Throws<Exception>(() => priceLevel.Fill(order1, 1100));
            Assert.Equal("Order quantity is less then requested fill quanity", ex.Message);
            Assert.Equal(2, priceLevel.OrderCount);
        }
    }
}
