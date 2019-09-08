using System;
using System.Collections.Generic;

namespace OrderMatcher
{
    public class BookSerializer : Serializer
    {
        private static readonly short version;
        private static readonly int messageLengthOffset;
        private static readonly int messageTypeOffset;
        private static readonly int versionOffset;
        private static readonly int timeStampOffset;
        private static readonly int ltpOffset;
        private static readonly int bidCountOffset;
        private static readonly int askCountOffset;

        private static readonly int sizeOfMessageLenght;
        private static readonly int sizeOfVersion;
        private static readonly int sizeOfMessageType;
        private static readonly int sizeOfTimeStamp;
        private static readonly int sizeOfPrice;
        private static readonly int sizeOfBidCount;
        private static readonly int sizeOfAskCount;
        private static readonly int bidStartOffset;

        private static readonly int sizeOfLevel;
        private static readonly int minMessageSize;

        static BookSerializer()
        {
            sizeOfMessageLenght = sizeof(int);
            sizeOfVersion = sizeof(short);
            sizeOfMessageType = sizeof(MessageType);
            sizeOfTimeStamp = sizeof(long);
            sizeOfPrice = Price.SizeOfPrice;
            sizeOfBidCount = sizeof(short);
            sizeOfAskCount = sizeof(short);
            sizeOfLevel = Price.SizeOfPrice + Quantity.SizeOfQuantity;
            minMessageSize = sizeOfMessageType + sizeOfVersion + sizeOfTimeStamp + sizeOfPrice + sizeOfBidCount + sizeOfAskCount;

            version = 1;

            messageLengthOffset = 0;
            messageTypeOffset = messageLengthOffset + sizeOfMessageLenght;
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
            var sizeOfMessage = sizeOfMessageLenght + sizeOfMessageType + sizeOfVersion + sizeOfTimeStamp + Price.SizeOfPrice + sizeOfAskCount + sizeOfBidCount + (sizeOfLevel * (bidCount + askCount));

            byte[] msg = new byte[sizeOfMessage];
            Write(msg, messageLengthOffset, sizeOfMessage);
            msg[messageTypeOffset] = (byte)MessageType.Book;
            Write(msg, versionOffset, version);
            Write(msg, timeStampOffset, timeStamp);
            Write(msg, ltpOffset, ltp ?? 0);
            Write(msg, bidCountOffset, bidCount);
            Write(msg, askCountOffset, askCount);

            int i = 0;
            foreach (var level in book.BidSide)
            {
                var start = bidStartOffset + (i * sizeOfLevel);
                Write(msg, start, level.Key);
                Write(msg, start + sizeOfPrice, level.Value.Quantity);
                if (++i == bidCount)
                {
                    break;
                }
            }

            i = 0;
            foreach (var level in book.AskSide)
            {
                var start = bidStartOffset + (bidCount * sizeOfLevel) + (i * sizeOfLevel);
                Write(msg, start, level.Key);
                Write(msg, start + sizeOfPrice, level.Value.Quantity);
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
