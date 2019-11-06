namespace OrderMatcher
{
    public enum MessageType : byte
    {
        //Input to order matcher
        NewOrderRequest = 1,
        CancelRequest = 2,
        BookRequest = 3,

        //Output from order matcher
        OrderMatchingResult = 101,
        Fill = 102,
        Cancel = 103,
        Book = 104,
        OrderTrigger = 105
    }
}
