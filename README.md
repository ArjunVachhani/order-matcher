  
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


## Code
```csharp
class Program
{
	static void Main(string[] args)
	{
		//create instance of matching engine.
		MatchingEngine matchingEngine = new MatchingEngine(0, 1, new MyTradeListener(), new TimeProvider());

		Order order1 = new Order { IsBuy = true, OrderId = 1, Quantity = 1000, Price = 10 };
		//push new order engine.
		matchingEngine.AddOrder(order1);

		//cancel existing orders
		matchingEngine.CancelOrder(1);//pass orderId to cancel
	}
}



//create a listener to receive events from matching engine. pass it to constructore of MatchingEngine
class MyTradeListener : ITradeListener
{
    public void OnCancel(ulong orderId, Quantity remainingQuantity, Quantity remainingOrderAmount, CancelReason cancelReason)
    {
        Console.WriteLine($"Order Cancelled.... orderId : {orderId}, remainingQuantity : {remainingQuantity}, cancelReason : {cancelReason}");
    }

    public void OnOrderTriggered(ulong orderId)
    {
        Console.WriteLine($"Stop Order Triggered.... orderId : {orderId}");
    }

    public void OnTrade(ulong incomingOrderId, ulong restingOrderId, Price matchPrice, Quantity matchQuantiy, bool incomingOrderCompleted)
    {
        Console.WriteLine($"Order matched.... incomingOrderId : {incomingOrderId}, restingOrderId : {restingOrderId}, executedQuantity : {matchQuantiy}, exetedPrice : {matchPrice}");
    }
}
```
