namespace OrderMatcher
{
    public interface ICancelListener
    {
        void OnCancel(ulong orderId, Quantity remainingQuantity, CancelReason cancelReason);
    }
}
