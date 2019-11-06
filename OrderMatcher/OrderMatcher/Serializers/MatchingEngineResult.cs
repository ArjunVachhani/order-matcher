namespace OrderMatcher.Serializers
{
    public class MatchingEngineResult
    {
        public ulong OrderId { get; set; }
        public OrderMatchingResult Result { get; set; }
        public long Timestamp { get; set; }
    }
}
