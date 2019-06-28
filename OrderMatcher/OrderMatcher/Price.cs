using System.Runtime.CompilerServices;

namespace OrderMatcher
{
    public struct Price
    {
        public const int SizeOfPrice = sizeof(int);
        private readonly int _price;
        public Price(int price)
        {
            _price = price;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Price(int price)
        {
            return new Price(price);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(Price c)
        {
            return c._price;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Price a, Price b)
        {
            return (a._price > b._price);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Price a, Price b)
        {
            return (a._price < b._price);
        }

        public static bool operator <=(Price a, Price b)
        {
            return (a._price <= b._price);
        }

        public static bool operator >=(Price a, Price b)
        {
            return (a._price >= b._price);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Price a, Price b)
        {
            return (a._price == b._price);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Price a, Price b)
        {
            return (a._price != b._price);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (!(obj is Price))
            {
                return false;
            }

            Price price = (Price)obj;
            return _price == price._price;
        }

        public override int GetHashCode()
        {
            return 326187671 + _price.GetHashCode();
        }
    }
}
