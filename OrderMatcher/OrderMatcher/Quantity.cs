namespace OrderMatcher
{
    public struct Quantity
    {
        public const int SizeOfQuantity = sizeof(int);
        private readonly int _quantity;
        public Quantity(int quantity)
        {
            _quantity = quantity;
        }

        public static Quantity operator -(Quantity a, Quantity b)
        {
            return (a._quantity - b._quantity);
        }

        public static Quantity operator +(Quantity a, Quantity b)
        {
            return (a._quantity + b._quantity);
        }

        public static implicit operator Quantity(int quantity)
        {
            return new Quantity(quantity);
        }

        public static implicit operator int(Quantity c)
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

        public override bool Equals(object obj)
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
    }
}
