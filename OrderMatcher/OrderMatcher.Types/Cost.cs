using OrderMatcher.Types.Serializers;
using System;

namespace OrderMatcher.Types
{
    public readonly struct Cost : IEquatable<Cost>, IComparable<Cost>
    {
        public const int SizeOfCost = sizeof(decimal);
        public static readonly Cost MaxValue = decimal.MaxValue;
        public static readonly Cost MinValue = decimal.MinValue;
        private readonly decimal _cost;

        public Cost(decimal cost)
        {
            _cost = cost;
        }

        public static implicit operator Cost(decimal cost)
        {
            return new Cost(cost);
        }

        public static implicit operator decimal(Cost c)
        {
            return c._cost;
        }

        public int CompareTo(Cost other)
        {
            return _cost.CompareTo(other._cost);
        }

        public bool Equals(Cost other)
        {
            return _cost == other._cost;
        }

        public override bool Equals(object obj)
        {
            return obj is Cost && ((Cost)obj)._cost == _cost;
        }

        public override int GetHashCode()
        {
            return 92834 + _cost.GetHashCode();
        }

        public static bool operator <(Cost left, Cost right)
        {
            return left._cost < right._cost;
        }

        public static bool operator <=(Cost left, Cost right)
        {
            return left._cost <= right._cost;
        }

        public static bool operator >(Cost left, Cost right)
        {
            return left._cost > right._cost;
        }

        public static bool operator >=(Cost left, Cost right)
        {
            return left._cost >= right._cost;
        }

        public static bool operator ==(Cost left, Cost right)
        {
            return left._cost == right._cost;
        }

        public static bool operator !=(Cost left, Cost right)
        {
            return left._cost != right._cost;
        }

        public void WriteBytes(Span<byte> bytes)
        {
            WriteBytes(bytes, this);
        }

        public static void WriteBytes(Span<byte> bytes, Cost cost)
        {
            Serializer.Write(bytes, cost._cost);
        }

        public static Cost ReadCost(ReadOnlySpan<byte> bytes)
        {
            return Serializer.ReadDecimal(bytes);
        }
    }
}
