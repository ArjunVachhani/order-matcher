using System;

namespace OrderMatcher
{
    public class OrderSerializer : Serializer
    {
        private static readonly short version;
        private static readonly int messageLengthOffset;
        private static readonly int messageTypeOffset;
        private static readonly int versionOffset;
        private static readonly int sideOffset;
        private static readonly int orderConditionOffset;
        private static readonly int orderIdOffset;
        private static readonly int priceOffset;
        private static readonly int quantityOffset;
        private static readonly int stopPriceOffset;
        private static readonly int totalQuantityOffset;
        private static readonly int cancelOnOffset;
        private static readonly int orderAmountOffset;

        private static readonly int sizeOfMessageLength;
        private static readonly int sizeOfMessage;
        private static readonly int sizeOfMessagetType;
        private static readonly int sizeOfOrderId;
        private static readonly int sizeOfVersion;
        private static readonly int sizeOfSide;
        private static readonly int sizeOfCancelOn;
        private static readonly int sizeOfOrderAmount;
        
        public static int MessageSize => sizeOfMessage;

        static OrderSerializer()
        {
            sizeOfMessageLength = sizeof(int);
            sizeOfOrderId = sizeof(ulong);
            sizeOfVersion = sizeof(short);
            sizeOfSide = sizeof(bool);
            sizeOfCancelOn = sizeof(long);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfOrderAmount = Quantity.SizeOfQuantity;
            version = 1;

            messageLengthOffset = 0;
            messageTypeOffset = messageLengthOffset + sizeOfMessageLength;
            versionOffset = messageTypeOffset + sizeOfMessagetType;
            sideOffset = versionOffset + sizeOfVersion;
            orderConditionOffset = sideOffset + sizeOfSide;
            orderIdOffset = orderConditionOffset + sizeof(OrderCondition);
            priceOffset = orderIdOffset + sizeOfOrderId;
            quantityOffset = priceOffset + Price.SizeOfPrice;
            stopPriceOffset = quantityOffset + Quantity.SizeOfQuantity;
            totalQuantityOffset = stopPriceOffset + Price.SizeOfPrice;
            cancelOnOffset = totalQuantityOffset + Quantity.SizeOfQuantity;
            orderAmountOffset = cancelOnOffset + sizeOfCancelOn;
            sizeOfMessage = orderAmountOffset + sizeOfOrderAmount;
        }

        public static byte[] Serialize(Order order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            byte[] msg = new byte[sizeOfMessage];
            Write(msg, messageLengthOffset, sizeOfMessage);
            msg[messageTypeOffset] = (byte)MessageType.NewOrderRequest;
            Write(msg, versionOffset, version);
            Write(msg, sideOffset, order.IsBuy);
            msg[orderConditionOffset] = (byte)order.OrderCondition;
            Write(msg, orderIdOffset, order.OrderId);
            Write(msg, priceOffset, order.Price);
            Write(msg, quantityOffset, order.Quantity);
            Write(msg, stopPriceOffset, order.StopPrice);
            Write(msg, totalQuantityOffset, order.TotalQuantity);
            Write(msg, cancelOnOffset, order.CancelOn);
            Write(msg, orderAmountOffset, order.OrderAmount);
            return msg;
        }

        public static Order Deserialize(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length != sizeOfMessage)
            {
                throw new Exception("Order Message must be of Size : " + sizeOfMessage);
            }

            var messageType = (MessageType)(bytes[messageTypeOffset]);
            if (messageType != MessageType.NewOrderRequest)
            {
                throw new Exception("Invalid Message");
            }

            var version = BitConverter.ToInt16(bytes, versionOffset);
            if (version != OrderSerializer.version)
            {
                throw new Exception("version mismatch");
            }

            var order = new Order();

            order.IsBuy = BitConverter.ToBoolean(bytes, sideOffset);
            order.OrderCondition = (OrderCondition)bytes[orderConditionOffset];
            order.OrderId = BitConverter.ToUInt64(bytes, orderIdOffset);
            order.Price = ReadPrice(bytes, priceOffset);
            order.Quantity = ReadQuantity(bytes, quantityOffset);
            order.StopPrice = ReadPrice(bytes, stopPriceOffset);
            order.TotalQuantity = ReadQuantity(bytes, totalQuantityOffset);
            order.CancelOn = BitConverter.ToInt64(bytes, cancelOnOffset);
            order.OrderAmount = ReadQuantity(bytes, orderAmountOffset);
            return order;
        }
    }
}
