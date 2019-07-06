using System.Runtime.CompilerServices;

namespace OrderMatcher
{
    public abstract class Serializer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteShort(byte[] array, int start, short value)
        {
            array[start] = (byte)value;
            array[start + 1] = (byte)(value >> 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt(byte[] array, int start, int value)
        {
            array[start] = (byte)value;
            array[start + 1] = (byte)(value >> 8);
            array[start + 2] = (byte)(value >> 16);
            array[start + 3] = (byte)(value >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLong(byte[] array, int start, long value)
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
        public static void WriteULong(byte[] array, int start, ulong value)
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
        public static void WriteBool(byte[] array, int start, bool value)
        {
            array[start] = (byte)(value ? 1 : 0);
        }
    }
}
