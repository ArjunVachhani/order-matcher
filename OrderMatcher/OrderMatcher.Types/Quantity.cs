using System;
using System.Diagnostics.CodeAnalysis;

namespace OrderMatcher.Types
{
    public readonly struct Quantity : IEquatable<Quantity>, IComparable<Quantity>
    {
        public const int SizeOfQuantity = sizeof(decimal);
        public static readonly Quantity MaxValue = decimal.MaxValue;
        public static readonly Quantity MinValue = decimal.MinValue;
        private readonly decimal _quantity;
        public Quantity(decimal quantity)
        {
            _quantity = quantity;
        }

        [SuppressMessage("Microsoft.Usage", "CA2225")]
        public static Quantity operator -(Quantity a, Quantity b)
        {
            return (a._quantity - b._quantity);
        }

        [SuppressMessage("Microsoft.Usage", "CA2225")]
        public static Quantity operator +(Quantity a, Quantity b)
        {
            return (a._quantity + b._quantity);
        }

        [SuppressMessage("Microsoft.Usage", "CA2225")]
        public static implicit operator Quantity(decimal quantity)
        {
            return new Quantity(quantity);
        }

        [SuppressMessage("Microsoft.Usage", "CA2225")]
        public static implicit operator decimal(Quantity c)
        {
            return c._quantity;
        }

        public static bool operator >(Quantity a, Quantity b)
        {
            return (a._quantity > b._quantity);
        }

        public static bool operator <(Quantity a, Quantity b)
        {
            return (a._quantity < b._quantity);
        }


        public static bool operator <=(Quantity a, Quantity b)
        {
            return (a._quantity <= b._quantity);
        }

        public static bool operator >=(Quantity a, Quantity b)
        {
            return (a._quantity >= b._quantity);
        }

        public static bool operator ==(Quantity a, Quantity b)
        {
            return (a._quantity == b._quantity);
        }

        public static bool operator !=(Quantity a, Quantity b)
        {
            return (a._quantity != b._quantity);
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is Quantity))
            {
                return false;
            }

            Quantity quantity = (Quantity)obj;
            return _quantity == quantity._quantity;
        }

        public override int GetHashCode()
        {
            return -5579697 + _quantity.GetHashCode();
        }

        [SuppressMessage("Microsoft.Globalization", "CA1305")]
        public override string ToString()
        {
            return _quantity.ToString();
        }

        public bool Equals(Quantity other)
        {
            return _quantity == other._quantity;
        }

        public int CompareTo(Quantity other)
        {
            return _quantity.CompareTo(other._quantity);
        }
    }
}
