using BenchmarkDotNet.Attributes;
using OrderMatcher.Types;
using OrderMatcher.Types.Serializers;
using System;

namespace OrderMatcher.Performance
{

    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn, AllStatisticsColumn]
    public class MatchingEngineBenchmark
    {
        [Benchmark]
        public void CreateMatchingEngine()
        {
            MatchingEngine engine = new MatchingEngine(null, null, 0.00000001m, 8);
        }

        [Benchmark]
        public void CreateOrder()
        {
            Order order = new Order()
            {
                CancelOn = 0,
                Cost = 0,
                Fee = 0,
                FeeId = 1,
                IsBuy = Random.Shared.Next() % 2 == 0,
                OpenQuantity = Random.Shared.Next(),
                OrderAmount = 0,
                OrderCondition = OrderCondition.None,
                OrderId = Random.Shared.Next(),
                Price = Random.Shared.Next(),
                //StopPrice = Random.Shared.Next(),
                TipQuantity = 0,
                UserId = Random.Shared.Next(),
            };
        }


        [Benchmark]
        public void AddOrder()
        {
            MatchingEngine engine = new MatchingEngine(null, null, 0.00000001m, 8);
            Order order = new Order()
            {
                CancelOn = 0,
                Cost = 0,
                Fee = 0,
                FeeId = 1,
                IsBuy = Random.Shared.Next() % 2 == 0,
                OpenQuantity = Random.Shared.Next(),
                OrderAmount = 0,
                OrderCondition = OrderCondition.None,
                OrderId = Random.Shared.Next(),
                Price = Random.Shared.Next(),
                //StopPrice = Random.Shared.Next(),
                TipQuantity = 0,
                UserId = Random.Shared.Next(),
            };

            var result = engine.AddOrder(order, Random.Shared.Next(), false);
            if (result != OrderMatchingResult.OrderAccepted)
                throw new Exception($"Oops {result}");
        }


        [Benchmark]
        public void AddAndCancelOrder()
        {
            MatchingEngine engine = new MatchingEngine(null, null, 0.00000001m, 8);
            Order order = new Order()
            {
                CancelOn = 0,
                Cost = 0,
                Fee = 0,
                FeeId = 1,
                IsBuy = Random.Shared.Next() % 2 == 0,
                OpenQuantity = Random.Shared.Next(),
                OrderAmount = 0,
                OrderCondition = OrderCondition.None,
                OrderId = Random.Shared.Next(),
                Price = Random.Shared.Next(),
                //StopPrice = Random.Shared.Next(),
                TipQuantity = 0,
                UserId = Random.Shared.Next(),
            };

            var result = engine.AddOrder(order, Random.Shared.Next(), false);
            if (result != OrderMatchingResult.OrderAccepted)
                throw new Exception($"Oops AddOrder : {result}");

            var cancelResult = engine.CancelOrder(order.OrderId);
            if (cancelResult != OrderMatchingResult.CancelAcepted)
                throw new Exception($"Oops CancelOrder : {result}");

        }


        [Benchmark]
        public void TenAddOrder()
        {
            MatchingEngine engine = new MatchingEngine(new FakeTradeListener(), new FakeFeeProvider(), 0.00000001m, 8);
            for (var i = 0; i < 10; i++)
            {
                Order order = new Order()
                {
                    CancelOn = 0,
                    Cost = 0,
                    Fee = 0,
                    FeeId = 1,
                    IsBuy = Random.Shared.Next() % 2 == 0,
                    OpenQuantity = Random.Shared.Next(),
                    OrderAmount = 0,
                    OrderCondition = OrderCondition.None,
                    OrderId = i,
                    Price = Random.Shared.Next(),
                    //StopPrice = Random.Shared.Next(),
                    TipQuantity = 0,
                    UserId = Random.Shared.Next(),
                };

                var result = engine.AddOrder(order, Random.Shared.Next(), false);
                if (result != OrderMatchingResult.OrderAccepted)
                    throw new Exception($"Oops {result}");
            }
        }

        [Benchmark]
        public void TenAddAndCancel()
        {
            MatchingEngine engine = new MatchingEngine(new FakeTradeListener(), new FakeFeeProvider(), 0.00000001m, 8);
            for (var i = 1; i <= 10; i++)
            {
                Order order = new Order()
                {
                    CancelOn = 0,
                    Cost = 0,
                    Fee = 0,
                    FeeId = 1,
                    IsBuy = Random.Shared.Next() % 2 == 0,
                    OpenQuantity = Random.Shared.Next(),
                    OrderAmount = 0,
                    OrderCondition = OrderCondition.None,
                    OrderId = i,
                    Price = Random.Shared.Next(),
                    //StopPrice = Random.Shared.Next(),
                    TipQuantity = 0,
                    UserId = Random.Shared.Next(),
                };

                var result = engine.AddOrder(order, Random.Shared.Next(), false);
                if (result != OrderMatchingResult.OrderAccepted)
                    throw new Exception($"Oops {result}");
            }

            for (int i = 1; i <= 10; i++)
            {
                engine.CancelOrder(i);
            }
        }
    }

    class FakeFeeProvider : IFeeProvider
    {
        public Fee GetFee(short feeId)
        {
            return new Fee { MakerFee = 0, TakerFee = 0 };
        }
    }

    class FakeTradeListener : ITradeListener
    {
        int messageSequence = 0;
        public void OnAccept(OrderId orderId, UserId userId)
        {
            var bytes = new byte[OrderAcceptSerializer.MessageSize];
            OrderAcceptSerializer.Serialize(++messageSequence, orderId, userId, 123, bytes);
        }

        public void OnCancel(OrderId orderId, UserId userId, Quantity remainingQuantity, Amount cost, Amount fee, CancelReason cancelReason)
        {
            var bytes = new byte[CancelledOrderSerializer.MessageSize];
            CancelledOrderSerializer.Serialize(++messageSequence, orderId, userId, remainingQuantity, cost, fee, cancelReason, 123, bytes);
        }

        public void OnOrderTriggered(OrderId orderId, UserId userId)
        {
            var bytes = new byte[OrderTriggerSerializer.MessageSize];
            OrderTriggerSerializer.Serialize(++messageSequence, orderId, userId, 123, bytes);
        }

        public void OnTrade(OrderId incomingOrderId, OrderId restingOrderId, UserId incomingUserId, UserId restingUserId, bool incomingOrderSide, Price matchPrice, Quantity matchQuantiy, Quantity? askRemainingQuantity, Amount? askFee, Amount? bidCost, Amount? bidFee)
        {
            var bytes = new byte[FillSerializer.MessageSize];
            FillSerializer.Serialize(++messageSequence, restingOrderId, incomingOrderId, restingUserId, incomingUserId, incomingOrderSide, matchPrice, matchQuantiy, askRemainingQuantity, askFee, bidCost, bidFee, 123, bytes);
        }
    }
}
