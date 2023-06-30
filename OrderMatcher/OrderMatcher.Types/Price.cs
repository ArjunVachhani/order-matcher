using System.Runtime.CompilerServices;

namespace OrderMatcher.Types;

public readonly struct Price : IEquatable<Price>, IComparable<Price>
{
    public const int SizeOfPrice = sizeof(decimal);
    public static readonly Price MaxValue = decimal.MaxValue;
    public static readonly Price MinValue = decimal.MinValue;
    private readonly decimal _price;
    public Price(decimal price)
    {
        _price = price;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Price(decimal price)
    {
        return new Price(price);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator decimal(Price c)
    {
        return c._price;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal decimal GetPrice()
    {
        return _price;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Price a, Price b)
    {
        return a._price > b._price;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Price a, Price b)
    {
        return a._price < b._price;
    }

    public static bool operator <=(Price a, Price b)
    {
        return a._price <= b._price;
    }

    public static bool operator >=(Price a, Price b)
    {
        return a._price >= b._price;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Price a, Price b)
    {
        return a._price == b._price;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Price a, Price b)
    {
        return a._price != b._price;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        if (!(obj is Price))
        {
            return false;
        }

        Price price = (Price)obj;
        return _price == price._price;
    }

    public bool Equals(Price other)
    {
        return _price == other._price;
    }

    public override int GetHashCode()
    {
        return 326187671 + _price.GetHashCode();
    }

    public override string ToString()
    {
        return _price.ToString();
    }

    public int CompareTo(Price other)
    {
        return _price.CompareTo(other._price);
    }

    public void WriteBytes(Span<byte> bytes)
    {
        WriteBytes(bytes, this);
    }

    public static void WriteBytes(Span<byte> bytes, Price price)
    {
        Serializer.Write(bytes, price._price);
    }

    public static Price ReadPrice(ReadOnlySpan<byte> bytes)
    {
        return Serializer.ReadDecimal(bytes);
    }
}
