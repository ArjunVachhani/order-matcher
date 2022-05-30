﻿using System.Diagnostics.CodeAnalysis;

namespace OrderMatcher.Types
{
    public class Order
    {
        public bool IsBuy { get; set; }
        public OrderId OrderId { get; set; }
        public ulong Sequence { get; set; }
        public Quantity OpenQuantity { get; set; }
        public Price Price { get; set; }
        public int CancelOn { get; set; }
        public Quantity Cost { get; set; }
        public Quantity Fee { get; set; }
        public short FeeId { get; set; }
        public Quantity TipQuantity { get; set; }
        public Quantity TotalQuantity { get; set; }
        public Quantity OrderAmount { get; set; }
        public Price StopPrice { get; set; }
        public OrderCondition OrderCondition { get; set; }
        public bool IsFilled
        {
            get
            {
                return OpenQuantity == 0;
            }
        }
        public bool IsStop
        {
            get
            {
                return StopPrice > 0;
            }
        }
        public bool IsTip
        {
            get
            {
                return TipQuantity > 0 && TotalQuantity > 0;
            }
        }
    }
}
