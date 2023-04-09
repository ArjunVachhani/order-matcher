namespace OrderMatcher.Types;

public readonly struct Amount : IEquatable<Amount>, IComparable<Amount>
{
    public const int SizeOfAmount = sizeof(decimal);
    public static readonly Amount MaxValue = decimal.MaxValue;
    public static readonly Amount MinValue = decimal.MinValue;
    private readonly decimal _amount;

    public Amount(decimal amount)
    {
        _amount = amount;
    }

    public static implicit operator Amount(decimal amount)
    {
        return new Amount(amount);
    }

    public static implicit operator decimal(Amount c)
    {
        return c._amount;
    }

    public int CompareTo(Amount other)
    {
        return _amount.CompareTo(other._amount);
    }

    public bool Equals(Amount other)
    {
        return _amount == other._amount;
    }

    public override string ToString()
    {
        return _amount.ToString();
    }

    public override bool Equals(object? obj)
    {
        return obj is Amount && ((Amount)obj)._amount == _amount;
    }

    public override int GetHashCode()
    {
        return 92834 + _amount.GetHashCode();
    }

    public static bool operator <(Amount left, Amount right)
    {
        return left._amount < right._amount;
    }

    public static bool operator <=(Amount left, Amount right)
    {
        return left._amount <= right._amount;
    }

    public static bool operator >(Amount left, Amount right)
    {
        return left._amount > right._amount;
    }

    public static bool operator >=(Amount left, Amount right)
    {
        return left._amount >= right._amount;
    }

    public static bool operator ==(Amount left, Amount right)
    {
        return left._amount == right._amount;
    }

    public static bool operator !=(Amount left, Amount right)
    {
        return left._amount != right._amount;
    }

    public void WriteBytes(Span<byte> bytes)
    {
        WriteBytes(bytes, this);
    }

    public static void WriteBytes(Span<byte> bytes, Amount amount)
    {
        Serializer.Write(bytes, amount._amount);
    }

    public static Amount ReadAmount(ReadOnlySpan<byte> bytes)
    {
        return Serializer.ReadDecimal(bytes);
    }
}
