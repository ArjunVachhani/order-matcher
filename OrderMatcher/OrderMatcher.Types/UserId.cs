using OrderMatcher.Types.Serializers;
using System;
using System.Runtime.CompilerServices;

namespace OrderMatcher.Types
{
    public readonly struct UserId : IEquatable<UserId>, IComparable<UserId>
    {
        public const int SizeOfUserId = sizeof(long);
        public static readonly UserId MaxValue = long.MaxValue;
        public static readonly UserId MinValue = long.MinValue;
        private readonly long _userId;

        public UserId(long userId)
        {
            _userId = userId;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UserId))
            {
                return false;
            }

            UserId userId = (UserId)obj;
            return _userId == userId._userId;
        }

        public override int GetHashCode()
        {
            return (423399 + _userId).GetHashCode();
        }

        public override string ToString()
        {
            return _userId.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UserId(long userId)
        {
            return new UserId(userId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(UserId userId)
        {
            return userId._userId;
        }

        public static bool operator ==(UserId a, UserId b)
        {
            return a._userId == b._userId;
        }

        public static bool operator !=(UserId a, UserId b)
        {
            return a._userId != b._userId;
        }

        public int CompareTo(UserId other)
        {
            return _userId.CompareTo(other._userId);
        }

        public bool Equals(UserId other)
        {
            return _userId == other._userId;
        }

        public void WriteBytes(Span<byte> bytes)
        {
            WriteBytes(bytes, this);
        }

        public static void WriteBytes(Span<byte> bytes, UserId userId)
        {
            Serializer.Write(bytes, userId._userId);
        }

        public static UserId ReadUserId(ReadOnlySpan<byte> bytes)
        {
            return Serializer.ReadLong(bytes);
        }
    }
}
