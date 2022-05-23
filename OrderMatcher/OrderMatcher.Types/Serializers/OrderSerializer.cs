﻿using System;

namespace OrderMatcher.Types.Serializers
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
        private static readonly int feeIdOffset;
        private static readonly int costOffset;
        private static readonly int feeOffset;

        private static readonly int sizeOfMessageLength;
        private static readonly int sizeOfMessage;
        private static readonly int sizeOfMessagetType;
        private static readonly int sizeOfOrderId;
        private static readonly int sizeOfVersion;
        private static readonly int sizeOfSide;
        private static readonly int sizeOfCancelOn;
        private static readonly int sizeOfOrderAmount;
        private static readonly int sizeOfFeeId;
        private static readonly int sizeOfCost;
        private static readonly int sizeOfFee;

        public static int MessageSize => sizeOfMessage;

        static OrderSerializer()
        {
            sizeOfMessageLength = sizeof(int);
            sizeOfOrderId = OrderId.SizeOfOrderId;
            sizeOfVersion = sizeof(short);
            sizeOfSide = sizeof(bool);
            sizeOfCancelOn = sizeof(int);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfOrderAmount = Quantity.SizeOfQuantity;
            sizeOfFeeId = sizeof(short);
            sizeOfCost = Quantity.SizeOfQuantity;
            sizeOfFee = Quantity.SizeOfQuantity;
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
            feeIdOffset = orderAmountOffset + sizeOfOrderAmount;
            costOffset = feeIdOffset + sizeOfFeeId;
            feeOffset = costOffset + sizeOfCost;
            sizeOfMessage = feeOffset + sizeOfFee;
        }

        public static void Serialize(Order order, Span<byte> bytes)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length < sizeOfMessage)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(bytes));

            Write(bytes.Slice(messageLengthOffset), sizeOfMessage);
            bytes[messageTypeOffset] = (byte)MessageType.NewOrderRequest;
            Write(bytes.Slice(versionOffset), version);
            Write(bytes.Slice(sideOffset), order.IsBuy);
            bytes[orderConditionOffset] = (byte)order.OrderCondition;
            Write(bytes.Slice(orderIdOffset), order.OrderId);
            Write(bytes.Slice(priceOffset), order.Price);

            if (order.TipQuantity > 0 && order.TotalQuantity > 0)
                Write(bytes.Slice(quantityOffset), order.TipQuantity);
            else
                Write(bytes.Slice(quantityOffset), order.OpenQuantity);

            Write(bytes.Slice(stopPriceOffset), order.StopPrice);
            Write(bytes.Slice(totalQuantityOffset), order.TotalQuantity);
            Write(bytes.Slice(cancelOnOffset), order.CancelOn);
            Write(bytes.Slice(orderAmountOffset), order.OrderAmount);
            Write(bytes.Slice(feeIdOffset), order.FeeId);
            Write(bytes.Slice(costOffset), order.Cost);
            Write(bytes.Slice(feeOffset), order.Fee);
        }

        public static Order Deserialize(ReadOnlySpan<byte> bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length != sizeOfMessage)
                throw new Exception("Order Message must be of Size : " + sizeOfMessage);

            var messageType = (MessageType)(bytes[messageTypeOffset]);

            if (messageType != MessageType.NewOrderRequest)
                throw new Exception(Constant.INVALID_MESSAGE);

            var version = BitConverter.ToInt16(bytes.Slice(versionOffset));

            if (version != OrderSerializer.version)
                throw new Exception(Constant.INVALID_VERSION);

            var order = new Order();

            order.IsBuy = BitConverter.ToBoolean(bytes.Slice(sideOffset));
            order.OrderCondition = (OrderCondition)bytes[orderConditionOffset];
            order.OrderId = BitConverter.ToInt32(bytes.Slice(orderIdOffset));
            order.Price = ReadPrice(bytes.Slice(priceOffset));
            order.OpenQuantity = ReadQuantity(bytes.Slice(quantityOffset));
            order.StopPrice = ReadPrice(bytes.Slice(stopPriceOffset));

            order.TotalQuantity = ReadQuantity(bytes.Slice(totalQuantityOffset));
            if (order.TotalQuantity > 0)
                order.TipQuantity = order.OpenQuantity;

            order.CancelOn = BitConverter.ToInt32(bytes.Slice(cancelOnOffset));
            order.OrderAmount = ReadQuantity(bytes.Slice(orderAmountOffset));
            order.FeeId = BitConverter.ToInt16(bytes.Slice(feeIdOffset));
            order.Cost = ReadQuantity(bytes.Slice(costOffset));
            order.Fee = ReadQuantity(bytes.Slice(feeOffset));
            return order;
        }
    }
}
