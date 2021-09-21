using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ChannelsPractice
{
    class Program
    {
        private static int counter = 0;
        static async Task Main(string[] args)
        {
            // Console.WriteLine("Hello World!");

            // var channel = Channel.CreateBounded<int>(4);
            // var channelReader = channel.Reader;
            // var channelWriter = channel.Writer;

            // for (int i = 0; i < 4; i++)
            // {
            //     Task.Run(ConsumerFactory(i, channelReader));
            // }

            // var value = 0;
            // while (true)
            // {
            //     await channelWriter.WaitToWriteAsync();
            //     await channelWriter.WriteAsync(value);
            //     Console.WriteLine($"Producer: pushed value: {value}");
            //     value = value + 1;
            // }

            //     Action<int> consumer = value =>
            //    {
            //        Console.WriteLine($"value: {value}");
            //    };


            //     var processor = new ConcurrentBatchProcessor<int>(4, Producer, consumer);
            //     await processor.Start();

            var nums = new int[] { 1, 2, 3, 4, 5, 6 };

            var index = 0;

            Func<Task<int>> producer = () =>
            {
                if (index < nums.Length)
                {
                    var counter = index;
                    index++;
                    return Task.Run(async () =>
                    {
                        await Task.Delay(500);
                        return nums[counter] * nums[counter];
                    });
                }
                return null;
                // return null;
            };

            var result = new List<int>();
            Action<int> accumulator = value =>
            {
                result.Add(value);
            };


            var processor = new ConcurrentBatchProcessorV2<int>(producer, accumulator, 4);
            await processor.Start();

            foreach (var value in result)
            {
                Console.WriteLine($"result: {value}");
            }
        }

        static int Producer()
        {
            return counter++;
        }

        static Action ConsumerFactory(int consumerNumber, ChannelReader<int> channelReader)
        {
            return async () =>
            {
                while (true)
                {
                    var value = await channelReader.ReadAsync();
                    await Task.Delay(1000);
                    Console.WriteLine($"From consumer: {consumerNumber}, value: {value}");
                }
            };
        }
    }


}
