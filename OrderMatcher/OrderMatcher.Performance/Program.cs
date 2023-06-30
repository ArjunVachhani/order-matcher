﻿using BenchmarkDotNet.Running;
using System.Threading;

namespace OrderMatcher.Performance;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($"Choose {Environment.NewLine}1 Load Runner {Environment.NewLine}2 Benchmark");
        int.TryParse(Console.ReadLine(), out var choice);

        if (choice == 1)
        {
            //use dotnet-counters to collect and monitor 
            LoadRunner loadRunner = new LoadRunner(100_000, 5, 3);
            Thread t = new Thread(loadRunner.Run);
            var cancellationTokenSource = new CancellationTokenSource();
            t.Start(cancellationTokenSource.Token);
            Console.WriteLine("Press any key to stop");
            Console.ReadLine();
            cancellationTokenSource.Cancel();
            t.Join();
        }
        else
        {

            BenchmarkRunner.Run<BookRequestDeserializeBenchmark>();
            BenchmarkRunner.Run<BookRequestSerializeBenchmark>();
            BenchmarkRunner.Run<CancelRequestDeserializeBenchmark>();
            BenchmarkRunner.Run<CancelRequestSerializeBenchmark>();
            BenchmarkRunner.Run<FillDeserializeBenchmark>();
            BenchmarkRunner.Run<FillSerializeBenchmark>();
            BenchmarkRunner.Run<OrderBookDeserializeBenchmark>();
            BenchmarkRunner.Run<OrderBookSerializeBenchmark>();
            BenchmarkRunner.Run<OrderCancelledDeserializeBenchmark>();
            BenchmarkRunner.Run<OrderCancelledSerializeBenchmark>();
            BenchmarkRunner.Run<OrderDeserializeBenchmark>();
            BenchmarkRunner.Run<OrderSerializeBenchmark>();
            BenchmarkRunner.Run<OrderSequenceComparerBenchmark>();
            BenchmarkRunner.Run<OrderTriggerDeserializeBenchmark>();
            BenchmarkRunner.Run<OrderTriggerSerializeBenchmark>();
            BenchmarkRunner.Run<PriceLevelBenchmark>();
            BenchmarkRunner.Run<OrderSequenceComparerBenchmark>();
            BenchmarkRunner.Run<SpanBenchmark>();
            BenchmarkRunner.Run<MatchingEngineBenchmark>();


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

[MessagePackObject]
public class Order2
{
    [Key(0)]
    public bool IsBuy { get; set; }

    [Key(1)]
    public ulong OrderId { get; set; }

    [IgnoreMember]
    public ulong Sequnce { get; set; }

    [Key(2)]
    public decimal Quantity { get; set; }

    [IgnoreMember]
    public decimal OpenQuantity { get; set; }

    [Key(3)]
    public decimal Price { get; set; }

    [Key(4)]
    public decimal StopPrice { get; set; }

    [Key(5)]
    public OrderCondition OrderCondition { get; set; }

    [Key(6)]
    public decimal TotalQuantity { get; set; }

    [Key(7)]
    public bool IsTip { get; set; }

    [Key(8)]
    public long CancelOn { get; set; }

    [Key(9)]
    public decimal OrderAmount { get; set; }

    [IgnoreMember]
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

[MessagePackObject]
public class BookRequest2
{
    [Key(0)]
    public int LevelCount { get; set; }
}
