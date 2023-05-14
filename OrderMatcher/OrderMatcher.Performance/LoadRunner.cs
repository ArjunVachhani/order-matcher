using Microsoft.Diagnostics.Runtime.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;

namespace OrderMatcher.Performance
{
    public class LoadRunner : ITradeListener
    {
        HashSet<OrderId> openOrders = new HashSet<OrderId>();
        private readonly MatchingEngine matchingEngine;


        private readonly int maxOpenOrdersCount;
        private readonly int marketOrderPercent;
        private readonly int stopOrderPercent;

        Stopwatch stopWatch = new Stopwatch();
        static Meter meter = new Meter("LoadRunner", "1.0.0");
        static Counter<int> messagesCounter = meter.CreateCounter<int>("messages-processed", "message");
        static Histogram<int> messagesHistogram = meter.CreateHistogram<int>("message-time-in-tics");
        static Histogram<int> ordersPlaceHistogram = meter.CreateHistogram<int>("order-place-time-in-tics");
        static Histogram<int> ordersCancelHistogram = meter.CreateHistogram<int>("order-cancel-time-in-tics");
        public LoadRunner(int maxOpenOrdersCount, int marketOrderPercent, int stopOrderPercent)
        {
            matchingEngine = new MatchingEngine(this, new PerfFeeProvider(), 0.00000001m, 2);
            this.maxOpenOrdersCount = maxOpenOrdersCount;
            this.marketOrderPercent = marketOrderPercent;
            this.stopOrderPercent = stopOrderPercent;
        }

        public void Run(object cancellationTokenObj)
        {
            CancellationToken cancellationToken = (CancellationToken)cancellationTokenObj;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (openOrders.Count >= maxOpenOrdersCount)
                    CancelOrder();
                else
                    PlaceOrder();
            }
        }

        private long i = 0;
        public void PlaceOrder()
        {

            var isMarket = Random.Shared.Next(0, 100) <= marketOrderPercent;
            var isStop = Random.Shared.Next(0, 100) <= stopOrderPercent;
            var random = Random.Shared.Next(1, 5000);
            var stopRandom = Random.Shared.Next(0, 5000);
            var isBuy = random % 2 == 0;
            var order = new Order
            {
                IsBuy = isBuy,
                OpenQuantity = random,
                FeeId = 1,
                Price = isMarket ? 0 : (isBuy ? 28500 - random : 27500 + random),
                StopPrice = isStop ? (isBuy ? 28500 - stopRandom : 27500 + stopRandom) : 0,
                OrderId = ++i
            };
            stopWatch.Restart();
            openOrders.Add(order.OrderId);
            var result = matchingEngine.AddOrder(order, 1);
            stopWatch.Stop();
            messagesCounter.Add(1);
            messagesHistogram.Record((int)stopWatch.ElapsedTicks);
            ordersPlaceHistogram.Record((int)stopWatch.ElapsedTicks);
            if (result != OrderMatchingResult.OrderAccepted)
            {
                Console.WriteLine("Oops " + result);
                openOrders.Remove(order.OrderId);
            }

        }

        public void CancelOrder()
        {
            var orderId = openOrders.First();
            stopWatch.Restart();
            var cancelResult = matchingEngine.CancelOrder(orderId);
            if (cancelResult != OrderMatchingResult.CancelAcepted)
            {
                Console.WriteLine("Oops " + cancelResult);
            }
            stopWatch.Stop();
            messagesCounter.Add(1);
            messagesHistogram.Record((int)stopWatch.ElapsedTicks);
            ordersCancelHistogram.Record((int)stopWatch.ElapsedTicks);
        }

        public void OnTrade(OrderId incomingOrderId, OrderId restingOrderId, UserId incomingUserId, UserId restingUserId, bool incomingOrderSide, Price matchPrice, Quantity matchQuantiy, Quantity? askRemainingQuantity, Amount? askFee, Amount? bidCost, Amount? bidFee)
        {
            var bidOrderId = incomingOrderSide ? incomingOrderId : restingOrderId;
            var askOrderId = incomingOrderSide ? restingOrderId : incomingOrderId;

            if (askRemainingQuantity != null)
            {
                openOrders.Remove(askOrderId);

                if (openOrders.Contains(askOrderId))
                {

                }
            }

            if (bidCost != null)
            {
                openOrders.Remove(bidOrderId);

                if (openOrders.Contains(bidOrderId))
                {

                }
            }
        }

        public void OnCancel(OrderId orderId, UserId userId, Quantity remainingQuantity, Amount cost, Amount fee, CancelReason cancelReason)
        {
            openOrders.Remove(orderId);
            if (openOrders.Contains(orderId))
            {

            }
        }

        public void OnAccept(OrderId orderId, UserId userId) { }

        public void OnOrderTriggered(OrderId orderId, UserId userId) { }
    }

    class PerfFeeProvider : IFeeProvider
    {
        public Fee GetFee(short feeId)
        {
            return new Fee() { MakerFee = 0.1m, TakerFee = 0.1m };
        }
    }
}
