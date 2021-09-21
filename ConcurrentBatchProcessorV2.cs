
using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ChannelsPractice
{
    public class ConcurrentBatchProcessorV2<T>
    {
        private readonly Action<T> _accumulator;
        private readonly Func<Task<T>> _tasksProducer;
        private readonly ChannelReader<Task<T>> _pendingTasksChannelReader;
        private readonly ChannelWriter<Task<T>> _pendingTasksChannelWriter;
        private readonly ChannelReader<T> _accumulatingChannelReader;
        private readonly ChannelWriter<T> _accumulatingChannelWriter;
        private readonly int _totalConcurrentProcessors;
        public ConcurrentBatchProcessorV2(Func<Task<T>> tasksProducer, Action<T> accumulator, int totalConcurrentProcessors)
        {
            // throw new NotmplementedException("Instead of taking IEnumerable<Task<T>>, We should use a producer that generates tasks on demand");
            if (tasksProducer is null)
            {
                throw new ArgumentNullException(nameof(tasksProducer));
            }

            if (accumulator is null)
            {
                throw new ArgumentNullException(nameof(accumulator));
            }
            if (totalConcurrentProcessors < 1 || totalConcurrentProcessors > 6)
            {
                throw new ArgumentException($"Invalid {nameof(totalConcurrentProcessors)}");
            }
            _tasksProducer = tasksProducer;
            _accumulator = accumulator;

            var tasksChannel = Channel.CreateBounded<Task<T>>(totalConcurrentProcessors);
            _pendingTasksChannelReader = tasksChannel.Reader;
            _pendingTasksChannelWriter = tasksChannel.Writer;


            var accumulatingChannel = Channel.CreateUnbounded<T>();
            _accumulatingChannelReader = accumulatingChannel.Reader;
            _accumulatingChannelWriter = accumulatingChannel.Writer;
            _totalConcurrentProcessors = totalConcurrentProcessors;
        }


        public async Task Start()
        {
            for (int i = 0; i < _totalConcurrentProcessors; i++)
            {
                Task.Run(ConsumerFactory(i));
            }

            Console.WriteLine("Consumers Started");


            var accumulatorTask = Task.Run(async () =>
            {
                Console.WriteLine($"inside acummulator reader started");
                while (await _accumulatingChannelReader.WaitToReadAsync())
                {
                    if (_accumulatingChannelReader.TryRead(out var message))
                    {
                        _accumulator(message);
                    }
                }
            });

            Console.WriteLine($"accumlator reader started");

            while (true)
            {
                var task = _tasksProducer();
                Console.WriteLine($"task produced");
                if (task is null)
                {
                    break;
                }
                await _pendingTasksChannelWriter.WaitToWriteAsync();
                await _pendingTasksChannelWriter.WriteAsync(task);
            }
            _pendingTasksChannelWriter.Complete();
            await accumulatorTask;

            // foreach (var task in _tasks)
            // {
            //     await _pendingTasksChannelWriter.WaitToWriteAsync();
            //     await _pendingTasksChannelWriter.WriteAsync(task);
            // }
        }

        private Action ConsumerFactory(int consumerNumber)
        {
            return async () =>
            {
                Console.WriteLine($"consumer: {consumerNumber} started");
                while (await _pendingTasksChannelReader.WaitToReadAsync())
                {
                    if (_pendingTasksChannelReader.TryRead(out var message))
                    {
                        var item = await message;
                        Console.WriteLine($"From consumer: {consumerNumber}, message resolved");
                        await _accumulatingChannelWriter.WriteAsync(item);
                    }
                }
                _accumulatingChannelWriter.TryComplete();
            };
        }
    }
}