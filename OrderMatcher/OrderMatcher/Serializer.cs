using System.Runtime.CompilerServices;

namespace OrderMatcher
{
    public abstract class Serializer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void CopyBytes(byte[] sourceArray, byte[] destinationArray, int destinationStartIndex)
        {
            for (int i = 0; i < sourceArray.Length; i++)
            {
                destinationArray[destinationStartIndex + i] = sourceArray[i];
            }
        }
    }
}
