namespace OrderMatcher
{
    public interface IOrderTriggerListener
    {
        void OnOrderTriggered(ulong orderId);
    }
}
