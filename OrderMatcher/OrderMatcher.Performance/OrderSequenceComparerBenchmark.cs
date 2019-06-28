using BenchmarkDotNet.Attributes;

namespace OrderMatcher.Performance
{
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public class OrderSequenceComparerBenchmark
    {
        private readonly OrderSequenceComparer _orderSequenceComparer;
        private readonly Order _order1;
        private readonly Order _order2;
        public OrderSequenceComparerBenchmark()
        {
            _orderSequenceComparer = new OrderSequenceComparer();

            _order1 = new Order() { Sequnce = 1 };
            _order2 = new Order() { Sequnce = 2 };
        }

        [Benchmark]
        public void OrderSequenceComparerGreaterThan()
        {
            _orderSequenceComparer.Compare(_order2, _order1);
        }

        [Benchmark]
        public void OrderSequenceComparerLessThan()
        {
            _orderSequenceComparer.Compare(_order1, _order2);
        }

        [Benchmark]
        public void OrderSequenceComparerEqual()
        {
            _orderSequenceComparer.Compare(_order1, _order2);
        }
    }
}
