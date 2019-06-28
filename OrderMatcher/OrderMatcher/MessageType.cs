namespace OrderMatcher
{
    public enum MessageType : byte
    {
        //Input to order matcher
        NewOrderRequest = 1,
        CancelRequest = 2,
        BookRequest = 3,

        //Output from order matcher
        Fill = 101,
        Cancel = 102,
        Book = 103,
        OrderTrigger = 104
    }
}
