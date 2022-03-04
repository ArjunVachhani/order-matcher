using System;
using System.Runtime.CompilerServices;

namespace OrderMatcher.Types.Serializers
{
    public abstract class Serializer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(Span<byte> array, short value)
        {
            if (array.Length <= 1)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(array));

            array[0] = (byte)value;
            array[1] = (byte)(value >> 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(Span<byte> array, int value)
        {
            if (array.Length <= 3)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(array));

            array[0] = (byte)value;
            array[1] = (byte)(value >> 8);
            array[2] = (byte)(value >> 16);
            array[3] = (byte)(value >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(Span<byte> array, long value)
        {
            if (array.Length <= 7)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(array));

            array[0] = (byte)value;
            array[1] = (byte)(value >> 8);
            array[2] = (byte)(value >> 16);
            array[3] = (byte)(value >> 24);
            array[4] = (byte)(value >> 32);
            array[5] = (byte)(value >> 40);
            array[6] = (byte)(value >> 48);
            array[7] = (byte)(value >> 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(Span<byte> array, ulong value)
        {
            if (array.Length <= 7)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(array));

            array[0] = (byte)value;
            array[1] = (byte)(value >> 8);
            array[2] = (byte)(value >> 16);
            array[3] = (byte)(value >> 24);
            array[4] = (byte)(value >> 32);
            array[5] = (byte)(value >> 40);
            array[6] = (byte)(value >> 48);
            array[7] = (byte)(value >> 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(Span<byte> array, bool value)
        {
            if (array.Length <= 0)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(array));

            array[0] = (byte)(value ? 1 : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(Span<byte> array, decimal value)
        {
            if (array.Length <= 15)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(array));

#if NET5_0
            Span<int> arr = stackalloc int[4];
            decimal.GetBits(value, arr);
#elif NETSTANDARD2_1
            var arr = decimal.GetBits(value);
#endif

            var lo = arr[0];
            var mid = arr[1];
            var hi = arr[2];
            var flags = arr[3];

            array[0] = (byte)lo;
            array[1] = (byte)(lo >> 8);
            array[2] = (byte)(lo >> 16);
            array[3] = (byte)(lo >> 24);

            array[4] = (byte)mid;
            array[5] = (byte)(mid >> 8);
            array[6] = (byte)(mid >> 16);
            array[7] = (byte)(mid >> 24);

            array[8] = (byte)hi;
            array[9] = (byte)(hi >> 8);
            array[10] = (byte)(hi >> 16);
            array[11] = (byte)(hi >> 24);

            array[12] = (byte)flags;
            array[13] = (byte)(flags >> 8);
            array[14] = (byte)(flags >> 16);
            array[15] = (byte)(flags >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Price ReadPrice(ReadOnlySpan<byte> array)
        {
            if (array.Length <= 15)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(array));

            int lo = (array[0]) | (array[1] << 8) | (array[2] << 16) | (array[3] << 24);
            int mid = (array[4]) | (array[5] << 8) | (array[6] << 16) | (array[7] << 24);
            int hi = (array[8]) | (array[9] << 8) | (array[10] << 16) | (array[11] << 24);
            int flags = (array[12]) | (array[13] << 8) | (array[14] << 16) | (array[15] << 24);
#if NET5_0
            Span<int> bits = stackalloc int[4];
            bits[0] = lo;
            bits[1] = mid;
            bits[2] = hi;
            bits[3] = flags;
            return new decimal(bits);
#elif NETSTANDARD2_1
            return new decimal(new[] { lo, mid, hi, flags });
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quantity ReadQuantity(ReadOnlySpan<byte> array)
        {
            if (array.Length <= 15)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(array));

            int lo = (array[0]) | (array[1] << 8) | (array[2] << 16) | (array[3] << 24);
            int mid = (array[4]) | (array[5] << 8) | (array[6] << 16) | (array[7] << 24);
            int hi = (array[8]) | (array[9] << 8) | (array[10] << 16) | (array[11] << 24);
            int flags = (array[12]) | (array[13] << 8) | (array[14] << 16) | (array[15] << 24);
#if NET5_0
            Span<int> bits = stackalloc int[4];
            bits[0] = lo;
            bits[1] = mid;
            bits[2] = hi;
            bits[3] = flags;
            return new decimal(bits);
#elif NETSTANDARD2_1
            return new decimal(new[] { lo, mid, hi, flags });
#endif
        }

        public static MessageType? GetMessageType(ReadOnlySpan<byte> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length < 7)
                return null;

            var messageSize = BitConverter.ToInt32(data.Slice(0));
            if (messageSize != data.Length)
                return null;

            if ((MessageType)data[4] == MessageType.NewOrderRequest && messageSize == OrderSerializer.MessageSize)
                return MessageType.NewOrderRequest;
            else if ((MessageType)data[4] == MessageType.CancelRequest && messageSize == CancelRequestSerializer.MessageSize)
                return MessageType.CancelRequest;
            else if ((MessageType)data[4] == MessageType.BookRequest && messageSize == BookRequestSerializer.MessageSize)
                return MessageType.BookRequest;
            else if ((MessageType)data[4] == MessageType.OrderMatchingResult && messageSize == MatchingEngineResultSerializer.MessageSize)
                return MessageType.OrderMatchingResult;
            else if ((MessageType)data[4] == MessageType.Fill && messageSize == FillSerializer.MessageSize)
                return MessageType.Fill;
            else if ((MessageType)data[4] == MessageType.Cancel && messageSize == CancelledOrderSerializer.MessageSize)
                return MessageType.Cancel;
            else if ((MessageType)data[4] == MessageType.OrderAccept && messageSize == OrderAcceptSerializer.MessageSize)
                return MessageType.OrderAccept;
            else if ((MessageType)data[4] == MessageType.OrderTrigger && messageSize == OrderTriggerSerializer.MessageSize)
                return MessageType.OrderTrigger;
            else if ((MessageType)data[4] == MessageType.Book)
                return MessageType.Book;

            return null;
        }
    }
}
