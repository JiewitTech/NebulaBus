using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NebulaBus.Transport.Memory
{
    internal class MemoryTransport : ITransport
    {
        public string Name => "Memory";
        private readonly NebulaOptions _nebulaOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentBag<ChannelInfo> _channelInfos;
        private readonly ILogger<MemoryTransport> _logger;

        public MemoryTransport(IServiceProvider serviceProvider, ILogger<MemoryTransport> logger)
        {
            _serviceProvider = serviceProvider;
            _nebulaOptions = serviceProvider.GetRequiredService<NebulaOptions>();
            _channelInfos = new ConcurrentBag<ChannelInfo>();
            _logger = logger;
        }

        public void Dispose()
        {
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            var nebulaHandlers = _serviceProvider.GetServices<INebulaHandler>();
            var handlerInfos = nebulaHandlers.Select(x => new HandlerInfo()
            {
                Name = x.Name,
                Group = x.Group,
                ExcuteThreadCount = x.ExecuteThreadCount.HasValue
                    ? x.ExecuteThreadCount.Value
                    : _nebulaOptions.ExecuteThreadCount,
                Type = x.GetType()
            });

            //start procedure and consumer
            foreach (var handler in handlerInfos.GroupBy(x => new { x.Name, x.Group }))
            {
                var channel = Channel.CreateUnbounded<(byte[] header, byte[] body)>();
                _channelInfos.Add(new ChannelInfo()
                {
                    Name = handler.Key.Name,
                    Group = handler.Key.Group,
                    Channel = channel
                });
            }

            //start consumer
            foreach (var handlerInfo in handlerInfos)
            {
                var channelInfo =
                    _channelInfos.FirstOrDefault(x => x.Name == handlerInfo.Name && x.Group == handlerInfo.Group);
                if (channelInfo == null) continue;
                RegistChannelConsumer(channelInfo, handlerInfo, cancellationToken);
            }

            await Task.CompletedTask;
        }

        public async Task Publish(string routingKey, object message, NebulaHeader header)
        {
            try
            {
                var channelInfoList =
                    _channelInfos.Where(x => x.Name == routingKey || x.Group == routingKey);

                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(message, _nebulaOptions.JsonSerializerOptions);
                var headerJsonBytes = JsonSerializer.SerializeToUtf8Bytes(header, _nebulaOptions.JsonSerializerOptions);
                foreach (var channelInfo in channelInfoList)
                    await channelInfo.Channel.Writer.WriteAsync((headerJsonBytes, jsonBytes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Processor RabbitmqProcessor publish message to {routingKey} failed");
                throw;
            }
        }

        private void RegistChannelConsumer(ChannelInfo channelInfo, HandlerInfo handlerInfo,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < _nebulaOptions.ExecuteThreadCount; i++)
            {
                _ = Task.Factory.StartNew(() =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if (channelInfo.Channel.Reader.TryRead(out var item))
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var handler = scope.ServiceProvider.GetService(handlerInfo.Type) as NebulaHandler;
                            if (handler != null)
                            {
                                var header = JsonSerializer.Deserialize<NebulaHeader>(item.header,
                                    _nebulaOptions.JsonSerializerOptions)!;
                                handler.Excute(scope.ServiceProvider, item.body, header).Wait();
                            }
                        }

                        Thread.Sleep(100);
                    }
                });
            }
        }
    }
}