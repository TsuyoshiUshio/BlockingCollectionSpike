
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BlockingCollectionSpike
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int capacity = 3;
            // Blocking Collection 
            var blockingCollection = new BlockingCollection<string>(boundedCapacity: capacity);
            CancellationTokenSource source = new CancellationTokenSource();

            Task producerThread = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    string command = Console.ReadLine();
                    if (command.Contains("quit"))
                    {
                        blockingCollection.CompleteAdding();
                        break;
                    }
                    if (command.Contains("cancel"))
                    {
                        Console.WriteLine("Cancelling ...");
                        source.Cancel();
                        break;
                    }
                    // blockingCollection.Add(command);   // blocked if it reach the capacity
                    if (blockingCollection.TryAdd(command, TimeSpan.FromSeconds(1)))
                    {
                           // it works!
                    }
                    else
                    {
                        Console.WriteLine($"It reached boundedCapacity: {capacity} couldn't add {command}");
                    }
                }
            });
            Task consumerAThread = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (blockingCollection.IsCompleted && blockingCollection.Count == 0) break;
                    string command = blockingCollection.Take();
                    Console.WriteLine($"ConsumerA: Take Received: {command}");
                    Task.Delay(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
                }
            });
            Task consumerBThread = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (blockingCollection.IsCompleted && blockingCollection.Count == 0) break;
                    string command;
                    try
                    {
                        if (blockingCollection.TryTake(out command, (int)TimeSpan.FromSeconds(5).TotalMilliseconds, source.Token))
                        {
                            Console.WriteLine($"ConsumerB: TryTake Received: {command}");
                        }
                        else
                        {
                            Console.WriteLine($"consumerB: Can't take now.");
                        }
                    } catch (OperationCanceledException e)
                    {
                        Console.WriteLine($"ConsumerB: Task is cancelled.: {e.Message}");
                        break;
                    }
                    Task.Delay(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
                }
            });
            Task.WaitAll(producerThread, consumerAThread, consumerBThread);
            Console.WriteLine("All done");
        }
    }
}
