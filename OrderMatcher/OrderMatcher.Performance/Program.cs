using BenchmarkDotNet.Running;

namespace OrderMatcher.Performance
{
    class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkRunner.Run<PriceComparerBenchmark>();
            //BenchmarkRunner.Run<OrderSequenceComparerBenchmark>();
            BenchmarkRunner.Run<SerializeBenchmark>();


            //TODO check for aggresive inline performance improvement
            //TODO check no foreach statement
            //TODO avoid inheritance / interface / virtual function
            //TODO order class field vs property performance
            //TODO performance check before and after getHashCode on Order, Price, Quantity, Cost
            //TODO check for data structure available O(1) O(log n)
            //TODO check for memory usage
            //TODO check use of property should be avoided as much as possible
            //TODO GC ??
        }
    }
}
