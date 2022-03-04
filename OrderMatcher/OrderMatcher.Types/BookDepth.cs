using System.Collections.Generic;

namespace OrderMatcher.Types
{
    public class BookDepth
    {
        public BookDepth(int timeStamp, Price? ltp, Dictionary<Price,Quantity> bid, Dictionary<Price, Quantity> ask)
        {
            TimeStamp = timeStamp;
            LTP = ltp;
            Bid = bid;
            Ask = ask;
        }

        public int TimeStamp { get; set; }
        public Price? LTP { get; set; }
        public Dictionary<Price, Quantity> Bid { get; }
        public Dictionary<Price, Quantity> Ask { get; }
    }
    
    public struct BookLevel
    {
        public Price Price { get; set; }
        public Quantity Quantity { get; set; }
    }
}
