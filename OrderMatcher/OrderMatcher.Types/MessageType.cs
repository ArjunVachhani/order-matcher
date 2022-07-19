namespace OrderMatcher.Types
{
    public enum MessageType : byte
    {
        //Input to order matcher
        NewOrderRequest = 1,
        CancelRequest = 2,
        BookRequest = 3,
        CancelOpenGTD = 4,

        //Output from order matcher
        OrderMatchingResult = 101,
        Fill = 102,
        Cancel = 103,
        Book = 104,
        OrderTrigger = 105,
        OrderAccept = 106,

        GeneralPurpose1 = 201,
        GeneralPurpose2 = 202,
        GeneralPurpose3 = 203,
        GeneralPurpose4 = 204,
        GeneralPurpose5 = 205,
    }
}
