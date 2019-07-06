using System;

namespace OrderMatcher
{
    public class OrderSerializer : Serializer
    {
        private static short version;
        private static int messageLengthOffset;
        private static int messageTypeOffset;
        private static int versionOffset;
        private static int sideOffset;
        private static int orderConditionOffset;
        private static int orderIdOffset;
        private static int priceOffset;
        private static int quantityOffset;
        private static int stopPriceOffset;
        private static int totalQuantityOffset;
        private static int cancelOnOffset;

        private static int sizeOfMessageLenght;
        private static int sizeOfMessage;
        private static int sizeOfMessagetType;
        private static int sizeOfOrderId;
        private static int sizeOfVersion;
        private static int sizeOfSide;
        private static int sizeOfCancelOn;

        static OrderSerializer()
        {
            sizeOfMessageLenght = sizeof(int);
            sizeOfOrderId = sizeof(ulong);
            sizeOfVersion = sizeof(short);
            sizeOfSide = sizeof(bool);
            sizeOfCancelOn = sizeof(ulong);
            sizeOfMessagetType = sizeof(MessageType);
            version = 1;

            messageLengthOffset = 0;
            messageTypeOffset = messageLengthOffset + sizeOfMessageLenght;
            versionOffset = messageTypeOffset + sizeOfMessagetType;
            sideOffset = versionOffset + sizeOfVersion;
            orderConditionOffset = sideOffset + sizeOfSide;
            orderIdOffset = orderConditionOffset + sizeof(OrderCondition);
            priceOffset = orderIdOffset + sizeOfOrderId;
            quantityOffset = priceOffset + Price.SizeOfPrice;
            stopPriceOffset = quantityOffset + Quantity.SizeOfQuantity;
            totalQuantityOffset = stopPriceOffset + Price.SizeOfPrice;
            cancelOnOffset = totalQuantityOffset + Quantity.SizeOfQuantity;
            sizeOfMessage = cancelOnOffset + sizeOfCancelOn;
        }

        public static byte[] Serialize(Order order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            byte[] msg = new byte[sizeOfMessage];
            WriteInt(msg, messageLengthOffset, sizeOfMessage);
            msg[messageTypeOffset] = (byte)MessageType.NewOrderRequest;
            var versionByteArray = BitConverter.GetBytes(version);
            msg[versionOffset] = versionByteArray[0];
            msg[versionOffset + 1] = versionByteArray[1];
            msg[sideOffset] = BitConverter.GetBytes(order.IsBuy)[0];
            msg[orderConditionOffset] = (byte)order.OrderCondition;
            WriteULong(msg, orderIdOffset, order.OrderId);
            WriteInt(msg, priceOffset, order.Price);
            WriteInt(msg, quantityOffset, order.Quantity);
            WriteInt(msg, stopPriceOffset, order.StopPrice);
            WriteInt(msg, totalQuantityOffset, order.TotalQuantity);
            WriteLong(msg, cancelOnOffset, order.CancelOn);
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
            order.Price = BitConverter.ToInt32(bytes, priceOffset);
            order.Quantity = BitConverter.ToInt32(bytes, quantityOffset);
            order.StopPrice = BitConverter.ToInt32(bytes, stopPriceOffset);
            order.TotalQuantity = BitConverter.ToInt32(bytes, totalQuantityOffset);
            order.CancelOn = BitConverter.ToInt64(bytes, cancelOnOffset);
            return order;
        }
    }
}
