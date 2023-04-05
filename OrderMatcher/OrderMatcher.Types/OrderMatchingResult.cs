namespace OrderMatcher.Types
{
    public enum OrderMatchingResult : byte
    {
        //Success Result
        OrderAccepted = 1,
        CancelAcepted = 2,

        //Failure Result
        OrderDoesNotExists = 11,
        InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity = 12,
        DuplicateOrder = 13,
        BookOrCancelCannotBeMarketOrStopOrder = 14,
        ImmediateOrCancelCannotBeStopOrder = 15,
        IcebergOrderCannotBeStopOrMarketOrder = 16,
        IcebergOrderCannotBeFOKorIOC = 17,
        InvalidIcebergOrderTotalQuantity = 18,
        FillOrKillCannotBeStopOrder = 19,
        InvalidCancelOnForGTD = 20,
        GoodTillDateCannotBeMarketOrIOCorFOK = 21,
        MarketOrderOnlySupportedOrderAmountOrQuantityNoBoth = 22,
        OrderAmountOnlySupportedForMarketBuyOrder = 23,
        QuantityAndTotalQuantityShouldBeMultipleOfStepSize = 24
    }
}
