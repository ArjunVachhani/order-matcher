using System.Collections.Generic;

namespace OrderMatcher
{
    public class BookDepth
    {
        public long TimeStamp { get; set; }
        public Price? LTP { get; set; }
        public List<KeyValuePair<Price, Quantity>> Bid { get; set; }
        public List<KeyValuePair<Price, Quantity>> Ask { get; set; }
    }
}
