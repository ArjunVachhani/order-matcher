using System.Runtime.CompilerServices;

namespace OrderMatcher
{
    public abstract class Serializer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte[] array, int start, short value)
        {
            array[start] = (byte)value;
            array[start + 1] = (byte)(value >> 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte[] array, int start, int value)
        {
            array[start] = (byte)value;
            array[start + 1] = (byte)(value >> 8);
            array[start + 2] = (byte)(value >> 16);
            array[start + 3] = (byte)(value >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte[] array, int start, long value)
        {
            array[start] = (byte)value;
            array[start + 1] = (byte)(value >> 8);
            array[start + 2] = (byte)(value >> 16);
            array[start + 3] = (byte)(value >> 24);
            array[start + 4] = (byte)(value >> 32);
            array[start + 5] = (byte)(value >> 40);
            array[start + 6] = (byte)(value >> 48);
            array[start + 7] = (byte)(value >> 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte[] array, int start, ulong value)
        {
            array[start] = (byte)value;
            array[start + 1] = (byte)(value >> 8);
            array[start + 2] = (byte)(value >> 16);
            array[start + 3] = (byte)(value >> 24);
            array[start + 4] = (byte)(value >> 32);
            array[start + 5] = (byte)(value >> 40);
            array[start + 6] = (byte)(value >> 48);
            array[start + 7] = (byte)(value >> 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte[] array, int start, bool value)
        {
            array[start] = (byte)(value ? 1 : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(byte[] array, int start, decimal value)
        {
            var arr = decimal.GetBits(value);
            var lo = arr[0];
            var mid = arr[1];
            var hi = arr[2];
            var flags = arr[3];

            array[start + 0] = (byte)lo;
            array[start + 1] = (byte)(lo >> 8);
            array[start + 2] = (byte)(lo >> 16);
            array[start + 3] = (byte)(lo >> 24);

            array[start + 4] = (byte)mid;
            array[start + 5] = (byte)(mid >> 8);
            array[start + 6] = (byte)(mid >> 16);
            array[start + 7] = (byte)(mid >> 24);

            array[start + 8] = (byte)hi;
            array[start + 9] = (byte)(hi >> 8);
            array[start + 10] = (byte)(hi >> 16);
            array[start + 11] = (byte)(hi >> 24);

            array[start + 12] = (byte)flags;
            array[start + 13] = (byte)(flags >> 8);
            array[start + 14] = (byte)(flags >> 16);
            array[start + 15] = (byte)(flags >> 24);
        }
    }
}
