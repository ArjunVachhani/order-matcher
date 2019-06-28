using BenchmarkDotNet.Attributes;

namespace OrderMatcher.Performance
{
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class PriceComparerBenchmark
    {
        private readonly PriceComparerDescending _priceComparerDescending;
        private readonly PriceComparerDescendingZeroFirst _priceComparerDescendingZeroFirst;
        private readonly PriceComparerAscending _priceComparerAscending;
        public PriceComparerBenchmark()
        {
            _priceComparerDescending = new PriceComparerDescending();
            _priceComparerDescendingZeroFirst = new PriceComparerDescendingZeroFirst();
            _priceComparerAscending = new PriceComparerAscending();
        }

        [Benchmark]
        public void PriceComparerAscendingGreaterThan()
        {
            _priceComparerAscending.Compare(2, 1);
        }

        [Benchmark]
        public void PriceComparerAscendingLessThan()
        {
            _priceComparerAscending.Compare(1, 2);
        }

        [Benchmark]
        public void PriceComparerAscendingEqual()
        {
            _priceComparerAscending.Compare(2, 2);
        }

        [Benchmark]
        public void PriceComparerDescendingGreaterThan()
        {
            _priceComparerDescending.Compare(2, 1);
        }

        [Benchmark]
        public void PriceComparerDescendingLessThan()
        {
            _priceComparerDescending.Compare(1, 2);
        }

        [Benchmark]
        public void PriceComparerDescendingEqual()
        {
            _priceComparerDescending.Compare(2, 2);
        }

        [Benchmark]
        public void PriceComparerDescendingZeroFirstGreaterThan()
        {
            _priceComparerDescendingZeroFirst.Compare(2, 1);
        }

        [Benchmark]
        public void PriceComparerDescendingZeroFirstLessThan()
        {
            _priceComparerDescendingZeroFirst.Compare(1, 2);
        }

        [Benchmark]
        public void PriceComparerDescendingZeroFirstEqual()
        {
            _priceComparerDescendingZeroFirst.Compare(2, 2);
        }

        [Benchmark]
        public void PriceComparerDescendingZeroFirst_Arg1_0_GreaterThan()
        {
            _priceComparerDescendingZeroFirst.Compare(0, 1);
        }

        [Benchmark]
        public void PriceComparerDescendingZeroFirst_Arg2_0_LessThan()
        {
            _priceComparerDescendingZeroFirst.Compare(1, 0);
        }

        [Benchmark]
        public void PriceComparerDescendingZeroFirst_Arg1_0_And_Arg2_0Equal()
        {
            _priceComparerDescendingZeroFirst.Compare(0, 0);
        }
    }
}

