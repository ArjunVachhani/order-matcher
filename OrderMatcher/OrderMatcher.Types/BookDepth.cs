using System.Collections.Generic;

namespace OrderMatcher.Types
{
    public class BookDepth
    {
        public BookDepth(int timeStamp, Price? ltp, List<KeyValuePair<Price, Quantity>> bid, List<KeyValuePair<Price, Quantity>> ask)
        {
            TimeStamp = timeStamp;
            LTP = ltp;
            Bid = bid;
            Ask = ask;
        }
        public int TimeStamp { get; private set; }
        public Price? LTP { get; private set; }
        public List<KeyValuePair<Price, Quantity>> Bid { get; private set; }
        public List<KeyValuePair<Price, Quantity>> Ask { get; private set; }
    }
}
