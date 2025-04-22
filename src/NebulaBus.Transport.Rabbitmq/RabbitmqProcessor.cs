using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaBus.Transport.Rabbitmq
{
    internal class RabbitmqProcessor : IProcessor
    {
        private readonly NebulaRabbitmqOptions _rabbitmqOptions;
        private readonly List<IChannel> _channels;
        private readonly ILogger<RabbitmqProcessor> _logger;
        private readonly IServiceProvider _serviceProvider;
        private bool _started;
        private readonly NebulaOptions _nebulaOptions;
        private readonly IRabbitmqChannelPool _channelPool;
        private CancellationToken _originalCancellationToken;

        public string Name => "Rabbitmq";

        public RabbitmqProcessor(
            IServiceProvider serviceProvider,
            IRabbitmqChannelPool rabbitmqChannelPool,
            NebulaRabbitmqOptions nebulaRabbitmqOptions,
            NebulaOptions nebulaOptions,
            ILogger<RabbitmqProcessor> logger)
        {
            _serviceProvider = serviceProvider;
            _nebulaOptions = nebulaOptions;
            _rabbitmqOptions = nebulaRabbitmqOptions;
            _channels = new List<IChannel>();
            _logger = logger;
            _channelPool = rabbitmqChannelPool;
        }

        public void Dispose()
        {
            try
            {
                foreach (var channel in _channels)
                {
                    channel.CloseAsync().Wait();
                    channel.Dispose();
                }
            }
            catch
            {
            }
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            _originalCancellationToken = cancellationToken;
            try
            {
                await RegisteConsumer(cancellationToken);
                _started = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Processor RabbitmqProcessor start failed");
            }
        }

        public async Task Publish(string routingKey, object message, NebulaHeader header)
        {
            var channel = await _channelPool.GetChannelAsync();
            try
            {
                var props = new BasicProperties()
                {
                    Headers = header.ToDictionary(x => x.Key, (x) => (object?)x.Value),
                    Persistent = true,
                    ContentType = "application/json"
                };

                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(message, _nebulaOptions.JsonSerializerOptions);
                await channel.BasicPublishAsync(_rabbitmqOptions.ExchangeName, routingKey, false, props, jsonBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Processor RabbitmqProcessor publish message to {routingKey} failed");
                throw ex;
            }
            finally
            {
                await _channelPool.ReturnChannel(channel);
            }
        }

        private async Task RegisteConsumer(CancellationToken cancellationToken)
        {
            var _nebulaHandlers = _serviceProvider.GetServices<INebulaHandler>();
            if (_nebulaHandlers == null) return;

            var handlerInfos = _nebulaHandlers.Select(x => new HandlerInfo()
            {
                Name = x.Name,
                Group = x.Group,
                ExcuteThreadCount = x.ExecuteThreadCount.HasValue
                    ? x.ExecuteThreadCount.Value
                    : _nebulaOptions.ExecuteThreadCount,
                Type = x.GetType()
            });

            foreach (var info in handlerInfos)
            {
                var getQos = _rabbitmqOptions.GetQos?.Invoke(info.Name, info.Group);
                var qos = getQos > 0 ? getQos : _rabbitmqOptions.Qos;

                for (byte i = 0; i < info.ExcuteThreadCount; i++)
                {
                    await RegisteConsumerByConfig(info.Name, info.Group, qos.Value, info.Type, cancellationToken);
                }
            }
        }

        private async Task RegisteConsumerByConfig(string name, string group, ushort qos, Type handlerType,
            CancellationToken cancellationToken)
        {
            var channel = await _channelPool.GetChannelAsync(cancellationToken);
            _channels.Add(channel);

            if (qos > 0)
                await channel.BasicQosAsync(0, qos, false);

            //Create Exchange
            await channel.ExchangeDeclareAsync(_rabbitmqOptions.ExchangeName, ExchangeType.Direct, true);
            //Create Queue
            await channel.QueueDeclareAsync(name, true, false, false, null);

            //Bind Group RoutingKey
            if (!string.IsNullOrEmpty(group))
                await channel.QueueBindAsync(name, _rabbitmqOptions.ExchangeName, group, null);

            //Bind Name RoutingKey
            if (!string.IsNullOrEmpty(name))
                await channel.QueueBindAsync(name, _rabbitmqOptions.ExchangeName, name, null);

            //Create Consumer
            var consumer = new NebulaRabbitmqConsumer(channel, _serviceProvider, handlerType);
            await channel.BasicConsumeAsync(name, false, $"{Guid.NewGuid()}", consumer, cancellationToken);
        }
    }
}