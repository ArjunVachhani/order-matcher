namespace OrderMatcher
{
    public class Order
    {
        public bool IsBuy { get; set; }
        public ulong OrderId { get; set; }
        public ulong Sequnce { get; set; }
        public Quantity Quantity { get; set; }
        public Quantity OpenQuantity { get; set; }
        public Price Price { get; set; }
        public Price StopPrice { get; set; }
        public OrderCondition OrderCondition { get; set; }
        public Quantity TotalQuantity { get; set; }
        public bool IsTip { get; set; }
        public long CancelOn { get; set; }
        public Quantity OrderAmount { get; set; }
        public bool IsFilled
        {
            get
            {
                if (IsBuy == true && Price == 0)
                {
                    if (OrderAmount == 0 && OpenQuantity == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return OpenQuantity == 0;
                }
            }
        }
    }
}
