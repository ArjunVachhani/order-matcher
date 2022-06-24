  
# order-matcher

![](https://github.com/ArjunVachhani/order-matcher/workflows/.NET%20Core/badge.svg?branch=master)

Order Matching Engine / Trading Engine

 - Built with .Net Core, can run on **linux and windows**
 - Support multiple order types
	 - Limit 
	 - Market  
	 - Stop Loss  / Stop Limit
	 - Immediate or Cancel(IOC) 
	 - Fill or kill(FOK) 
	 - Good till Date(GTD) 
	 - Iceberg
       


**Supports integer & real numbers/decimal for price and quantity**

**Hand written serializer faster than any serializer. x15 times faster than JSON, x5 times faster than messagepack**

## Documentation
[home](https://github.com/ArjunVachhani/order-matcher/wiki)

[1. Terminology](https://github.com/ArjunVachhani/order-matcher/wiki/1.-Terminology)

[2. Order](https://github.com/ArjunVachhani/order-matcher/wiki/2.-Order)

[3. Frequently Asked Questions(FAQ)](https://github.com/ArjunVachhani/order-matcher/wiki/FAQ----Frequently-Asked-Questions)

## Code
```csharp

    class Program
    {
        static void Main(string[] args)
        {
            //timeProvider will provide epoch 
            var timeProvider = new TimeProvider();

            //create instance of matching engine.
            MatchingEngine matchingEngine = new MatchingEngine(new MyTradeListener(), new MyFeeProvider(), new Quantity(0.0000_0001m), 8);

            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 0.01m, Price = 69_000 };
            //push new order engine.
            var addResult = matchingEngine.AddOrder(order1, timeProvider.GetSecondsFromEpoch());
            if(addResult == OrderMatchingResult.OrderAccepted)
            {
                // matching engine has accepted order
            }

            //cancel existing orders
            var cancelResult = matchingEngine.CancelOrder(1);//pass orderId to cancel
            if (cancelResult == OrderMatchingResult.CancelAcepted)
            {
                // cancel request is accepted
            }
        }
    }



    //create a listener to receive events from matching engine. pass it to constructore of MatchingEngine
    class MyTradeListener : ITradeListener
    {
        public void OnAccept(OrderId orderId)
        {
            Console.WriteLine($"Order Accepted.... orderId : {orderId}");
        }

        public void OnCancel(OrderId orderId, Quantity remainingQuantity, Quantity cost, Quantity fee, CancelReason cancelReason)
        {
            Console.WriteLine($"Order Cancelled.... orderId : {orderId}, remainingQuantity : {remainingQuantity}, cancelReason : {cancelReason}");
        }

        public void OnOrderTriggered(OrderId orderId)
        {
            Console.WriteLine($"Stop Order Triggered.... orderId : {orderId}");
        }

        public void OnTrade(OrderId incomingOrderId, OrderId restingOrderId, Price matchPrice, Quantity matchQuantiy, Quantity? askRemainingQuantity, Quantity? askFee, Quantity? bidCost, Quantity? bidFee)
        {
            if (bidCost.HasValue)
            {
                // buy order completed
            }
            if (askRemainingQuantity.HasValue)
            {
                // sell order completed
            }

            Console.WriteLine($"Order matched.... incomingOrderId : {incomingOrderId}, restingOrderId : {restingOrderId}, executedQuantity : {matchQuantiy}, exetedPrice : {matchPrice}");
        }
    }

    class MyFeeProvider : IFeeProvider
    {
        public Fee GetFee(short feeId)
        {
            return new Fee
            {
                TakerFee = 0.5m, //0.5% taker fee
                MakerFee = 0.1m, //0.1% maker fee
            };
        }
    }
```
