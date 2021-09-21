
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ChannelsPractice
{
    public class ConcurrentBatchProcessor<T>
    {
        private readonly ChannelWriter<T> _writer;
        private readonly ChannelReader<T> _reader;
        private readonly Func<T> _producer;
        private readonly Action<T> _consumer;
        private readonly int _consumers;
        public ConcurrentBatchProcessor(int consumes, Func<T> producer, Action<T> consumer)
        {
            _consumers = consumes;
            var channel = Channel.CreateBounded<T>(consumes);
            _writer = channel.Writer;
            _reader = channel.Reader;
            _producer = producer;
            _consumer = consumer;
        }

        public async Task Start()
        {
            for (int i = 0; i < _consumers; i++)
            {
                Task.Run(ConsumerFactory(i));
            }

            while (true)
            {
                await _writer.WaitToWriteAsync();
                await _writer.WriteAsync(_producer());
            }
        }

        Action ConsumerFactory(int conNum)
        {
            return async () =>
            {
                var consumerNum = conNum;
                while (true)
                {
                    var value = await _reader.ReadAsync();
                    Console.WriteLine($"Consumer {consumerNum} awoke");
                    _consumer(value);
                    await Task.Delay(1000);
                }
            };
        }

    }
}