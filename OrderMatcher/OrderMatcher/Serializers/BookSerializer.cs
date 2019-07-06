using System;
using System.Collections.Generic;

namespace OrderMatcher
{
    public class BookSerializer : Serializer
    {
        private static short version;
        private static int messageTypeOffset;
        private static int versionOffset;
        private static int timeStampOffset;
        private static int ltpOffset;
        private static int bidCountOffset;
        private static int askCountOffset;

        private static int sizeOfVersion;
        private static int sizeOfMessageType;
        private static int sizeOfTimeStamp;
        private static int sizeOfPrice;
        private static int sizeOfBidCount;
        private static int sizeOfAskCount;
        private static int bidStartOffset;

        private static int sizeOfLevel;
        private static int minMessageSize;

        static BookSerializer()
        {
            sizeOfVersion = sizeof(short);
            sizeOfMessageType = sizeof(MessageType);
            sizeOfTimeStamp = sizeof(long);
            sizeOfPrice = Price.SizeOfPrice;
            sizeOfBidCount = sizeof(short);
            sizeOfAskCount = sizeof(short);
            sizeOfLevel = Price.SizeOfPrice + Quantity.SizeOfQuantity;
            minMessageSize = sizeOfMessageType + sizeOfVersion + sizeOfTimeStamp + sizeOfPrice + sizeOfBidCount + sizeOfAskCount;

            version = 1;

            messageTypeOffset = 0;
            versionOffset = messageTypeOffset + sizeOfMessageType;
            timeStampOffset = versionOffset + sizeOfVersion;
            ltpOffset = timeStampOffset + sizeOfTimeStamp;
            bidCountOffset = ltpOffset + sizeOfPrice;
            askCountOffset = bidCountOffset + sizeOfBidCount;
            bidStartOffset = askCountOffset + sizeOfAskCount;
        }

        public static byte[] Serialize(Book book, int levels, Price? ltp, long timeStamp)
        {
            if (book == null)
            {
                throw new ArgumentNullException(nameof(book));
            }

            if (levels < 0)
            {
                throw new ArgumentException("levels should be non negative");
            }

            var bidCount = (short)(book.BidPriceLevelCount < levels ? book.BidPriceLevelCount : levels);
            var askCount = (short)(book.AskPriceLevelCount < levels ? book.AskPriceLevelCount : levels);
            var sizeOfMessage = sizeOfMessageType + sizeOfVersion + sizeOfTimeStamp + Price.SizeOfPrice + sizeOfAskCount + sizeOfBidCount + (sizeOfLevel * (bidCount + askCount));

            byte[] msg = new byte[sizeOfMessage];
            msg[messageTypeOffset] = (byte)MessageType.Book;
            WriteShort(msg, versionOffset, version);
            WriteLong(msg, timeStampOffset, timeStamp);
            WriteInt(msg, ltpOffset, ltp ?? 0);
            WriteShort(msg, bidCountOffset, bidCount);
            WriteShort(msg, askCountOffset, askCount);

            int i = 0;
            foreach (var level in book.BidSide)
            {
                var start = bidStartOffset + (i * sizeOfLevel);
                WriteInt(msg, start, level.Key);
                WriteInt(msg, start + sizeOfPrice, level.Value.Quantity);
                if (++i == bidCount)
                {
                    break;
                }
            }

            i = 0;
            foreach (var level in book.AskSide)
            {
                var start = bidStartOffset + (bidCount * sizeOfLevel) + (i * sizeOfLevel);
                WriteInt(msg, start, level.Key);
                WriteInt(msg, start + sizeOfPrice, level.Value.Quantity);
                if (++i == askCount)
                {
                    break;
                }
            }
            return msg;
        }

        public static BookDepth Deserialize(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length < minMessageSize)
            {
                throw new Exception("Book Message must be greater than of Size : " + minMessageSize);
            }

            var messageType = (MessageType)(bytes[messageTypeOffset]);
            if (messageType != MessageType.Book)
            {
                throw new Exception("Invalid Message");
            }

            var version = BitConverter.ToInt16(bytes, versionOffset);
            if (version != BookSerializer.version)
            {
                throw new Exception("version mismatch");
            }

            var book = new BookDepth();
            book.TimeStamp = BitConverter.ToInt64(bytes, timeStampOffset);
            book.LTP = BitConverter.ToInt32(bytes, ltpOffset);
            if (book.LTP == 0)
            {
                book.LTP = null;
            }

            var bidCount = BitConverter.ToInt16(bytes, bidCountOffset);
            var askCount = BitConverter.ToInt16(bytes, askCountOffset);

            List<KeyValuePair<Price, Quantity>> bid = new List<KeyValuePair<Price, Quantity>>(bidCount);
            List<KeyValuePair<Price, Quantity>> ask = new List<KeyValuePair<Price, Quantity>>(askCount);
            for (int i = 0; i < bidCount; i++)
            {
                var price = BitConverter.ToInt32(bytes, bidStartOffset + (i * sizeOfLevel));
                var quantity = BitConverter.ToInt32(bytes, bidStartOffset + (i * sizeOfLevel) + sizeOfPrice);
                bid.Add(new KeyValuePair<Price, Quantity>(price, quantity));
            }
            var askStartOffset = bidStartOffset + (bidCount * sizeOfLevel);
            for (int i = 0; i < askCount; i++)
            {
                var price = BitConverter.ToInt32(bytes, askStartOffset + (i * sizeOfLevel));
                var quantity = BitConverter.ToInt32(bytes, askStartOffset + (i * sizeOfLevel) + sizeOfPrice);
                ask.Add(new KeyValuePair<Price, Quantity>(price, quantity));
            }
            book.Bid = bid;
            book.Ask = ask;
            return book;
        }
    }
}
