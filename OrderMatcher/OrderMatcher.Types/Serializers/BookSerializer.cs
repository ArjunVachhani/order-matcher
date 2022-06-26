using System;
using System.Collections.Generic;

namespace OrderMatcher.Types.Serializers
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

        private static readonly int sizeOfMessageLength;
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
            sizeOfMessageLength = sizeof(int);
            sizeOfVersion = sizeof(short);
            sizeOfMessageType = sizeof(MessageType);
            sizeOfTimeStamp = sizeof(int);
            sizeOfPrice = Price.SizeOfPrice;
            sizeOfBidCount = sizeof(short);
            sizeOfAskCount = sizeof(short);
            sizeOfLevel = Price.SizeOfPrice + Quantity.SizeOfQuantity;
            minMessageSize = sizeOfMessageLength + sizeOfMessageType + sizeOfVersion + sizeOfTimeStamp + sizeOfPrice + sizeOfBidCount + sizeOfAskCount;

            version = 1;

            messageLengthOffset = 0;
            messageTypeOffset = messageLengthOffset + sizeOfMessageLength;
            versionOffset = messageTypeOffset + sizeOfMessageType;
            timeStampOffset = versionOffset + sizeOfVersion;
            ltpOffset = timeStampOffset + sizeOfTimeStamp;
            bidCountOffset = ltpOffset + sizeOfPrice;
            askCountOffset = bidCountOffset + sizeOfBidCount;
            bidStartOffset = askCountOffset + sizeOfAskCount;
        }

        public static int GetMessageSize(BookDepth book)
        {
            if (book == null)
                throw new ArgumentNullException(nameof(book));

            var bidCount = (short)(book.Bid.Count);
            var askCount = (short)(book.Ask.Count);
            var sizeOfMessage = sizeOfMessageLength + sizeOfMessageType + sizeOfVersion + sizeOfTimeStamp + Price.SizeOfPrice + sizeOfAskCount + sizeOfBidCount + (sizeOfLevel * (bidCount + askCount));
            return sizeOfMessage;
        }

        public static void Serialize(BookDepth book, Span<byte> bytes)
        {
            if (book == null)
                throw new ArgumentNullException(nameof(book));

            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            var sizeOfMessage = GetMessageSize(book);

            if (bytes.Length != sizeOfMessage)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(book));

            Write(bytes.Slice(messageLengthOffset), sizeOfMessage);
            bytes[messageTypeOffset] = (byte)MessageType.Book;
            Write(bytes.Slice(versionOffset), version);
            Write(bytes.Slice(timeStampOffset), book.TimeStamp);
            Price.WriteBytes(bytes.Slice(ltpOffset), book.LTP ?? 0);
            Write(bytes.Slice(bidCountOffset), (short)book.Bid.Count);
            Write(bytes.Slice(askCountOffset), (short)book.Ask.Count);

            int i = 0;
            foreach (var level in book.Bid)
            {
                var start = bidStartOffset + (i * sizeOfLevel);
                Price.WriteBytes(bytes.Slice(start), level.Key);
                Quantity.WriteBytes(bytes.Slice(start + sizeOfPrice), level.Value);
                if (++i == book.Bid.Count)
                {
                    break;
                }
            }

            i = 0;
            foreach (var level in book.Ask)
            {
                var start = bidStartOffset + (book.Bid.Count * sizeOfLevel) + (i * sizeOfLevel);
                Price.WriteBytes(bytes.Slice(start), level.Key);
                Quantity.WriteBytes(bytes.Slice(start + sizeOfPrice), level.Value);
                if (++i == book.Ask.Count)
                {
                    break;
                }
            }
        }

        public static BookDepth Deserialize(ReadOnlySpan<byte> bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length < minMessageSize)
                throw new Exception("Book Message must be greater than of Size : " + minMessageSize);

            var messageType = (MessageType)(bytes[messageTypeOffset]);
            if (messageType != MessageType.Book)
            {
                throw new Exception(Constant.INVALID_MESSAGE);
            }

            var version = BitConverter.ToInt16(bytes.Slice(versionOffset));
            if (version != BookSerializer.version)
            {
                throw new Exception(Constant.INVALID_VERSION);
            }

            var timeStamp = BitConverter.ToInt32(bytes.Slice(timeStampOffset));
            Price? ltp = Price.ReadPrice(bytes.Slice(ltpOffset));
            if (ltp == 0)
            {
                ltp = null;
            }


            var bidCount = BitConverter.ToInt16(bytes.Slice(bidCountOffset));
            var askCount = BitConverter.ToInt16(bytes.Slice(askCountOffset));

            Dictionary<Price, Quantity> bid = new Dictionary<Price, Quantity>(bidCount);
            Dictionary<Price, Quantity> ask = new Dictionary<Price, Quantity>(askCount);
            for (int i = 0; i < bidCount; i++)
            {
                var price = Price.ReadPrice(bytes.Slice(bidStartOffset + (i * sizeOfLevel)));
                var quantity = Quantity.ReadQuantity(bytes.Slice(bidStartOffset + (i * sizeOfLevel) + sizeOfPrice));
                bid.Add(price, quantity);
            }
            var askStartOffset = bidStartOffset + (bidCount * sizeOfLevel);
            for (int i = 0; i < askCount; i++)
            {
                var price = Price.ReadPrice(bytes.Slice(askStartOffset + (i * sizeOfLevel)));
                var quantity = Quantity.ReadQuantity(bytes.Slice(askStartOffset + (i * sizeOfLevel) + sizeOfPrice));
                ask.Add(price, quantity);
            }

            var book = new BookDepth(timeStamp, ltp, bid, ask);
            return book;
        }
    }
}
