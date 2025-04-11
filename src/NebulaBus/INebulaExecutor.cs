using NebulaBus.Scheduler;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NebulaBus
{
    internal class NebulaExecutor<T>
    {
        private readonly Channel<(T id, string message, NebulaHeader header)> _channel;
        private readonly byte _threadCount;
        private readonly NebulaHandler _handler;
        private readonly IProcessor _processor;
        private readonly IDelayMessageScheduler _delayMessageScheduler;
        private readonly SemaphoreSlim _semaphore;

        public NebulaExecutor(IProcessor processor, NebulaHandler nebulaHandler, IDelayMessageScheduler delayMessageScheduler)
        {
            _processor = processor;
            _delayMessageScheduler = delayMessageScheduler;
            _threadCount = nebulaHandler.ExecuteThreadCount;
            _handler = nebulaHandler;
            _channel = Channel.CreateUnbounded<(T, string, NebulaHeader)>();
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public async Task Enqueue(T id, string message, NebulaHeader header)
        {
            await _channel.Writer.WriteAsync((id, message, header));
        }

        public void Start(CancellationToken cancellationToken, Func<T, ValueTask> callback)
        {
            for (int i = 0; i < _threadCount; i++)
            {
                Task.Run(async () =>
                {
                    while (await _channel.Reader.WaitToReadAsync())
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;
                        if (_channel.Reader.TryRead(out var data))
                        {
                            await _handler.Excute(_processor, _delayMessageScheduler, data.message, data.header);
                            await CallBack(data.id, callback);
                        }
                    }
                });
            }
        }

        private async Task CallBack(T id, Func<T, ValueTask> callback)
        {
            await _semaphore.WaitAsync();
            try
            {
                await callback(id);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}