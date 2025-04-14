using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaBus.Rabbitmq
{
    internal class RabbitmqProcessor : IProcessor
    {
        private readonly RabbitmqOptions _rabbitmqOptions;
        private readonly List<IChannel> _channels;
        private readonly ILogger<RabbitmqProcessor> _logger;
        private readonly IServiceProvider _serviceProvider;
        private bool _started;
        private readonly NebulaOptions _nebulaOptions;
        private readonly IRabbitmqChannelPool _channelPool;

        public RabbitmqProcessor(
            IServiceProvider serviceProvider,
            IRabbitmqChannelPool rabbitmqChannelPool,
            NebulaOptions nebulaOptions,
            ILogger<RabbitmqProcessor> logger)
        {
            _serviceProvider = serviceProvider;
            _nebulaOptions = nebulaOptions;
            _rabbitmqOptions = nebulaOptions.RabbitmqOptions;
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
            { }
        }

        public async Task Start(CancellationToken cancellationToken)
        {
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
                ExcuteThreadCount = x.ExecuteThreadCount.HasValue ? x.ExecuteThreadCount.Value : _nebulaOptions.ExecuteThreadCount,
                Type = x.GetType()
            });

            foreach (var info in handlerInfos)
            {
                var getQos = _rabbitmqOptions.GetQos?.Invoke(info.Name, info.Group);
                var qos = getQos > 0 ? getQos : _rabbitmqOptions.Qos;

                await RegisteConsumerByConfig(info, qos.Value, cancellationToken);
            }
        }

        private async Task RegisteConsumerByConfig(HandlerInfo handlerInfo, ushort qos, CancellationToken cancellationToken)
        {
            //每个handler创建一个channel 一个consumer
            for (byte i = 0; i < handlerInfo.ExcuteThreadCount; i++)
            {
                var channel = await _channelPool.GetChannelAsync(cancellationToken);
                _channels.Add(channel);

                if (qos > 0)
                    await channel.BasicQosAsync(0, qos, false);

                //Create Exchange
                await channel.ExchangeDeclareAsync(_rabbitmqOptions.ExchangeName, ExchangeType.Direct, true);
                //Create Queue
                await channel.QueueDeclareAsync(handlerInfo.Name, true, false, false, null);

                //Bind Group RoutingKey
                if (!string.IsNullOrEmpty(handlerInfo.Group))
                    await channel.QueueBindAsync(handlerInfo.Name, _rabbitmqOptions.ExchangeName, handlerInfo.Group, null);

                //Bind Name RoutingKey
                if (!string.IsNullOrEmpty(handlerInfo.Name))
                    await channel.QueueBindAsync(handlerInfo.Name, _rabbitmqOptions.ExchangeName, handlerInfo.Name, null);

                //Create Consumer
                var consumer = new NebulaRabbitmqConsumer(channel, _serviceProvider, handlerInfo.Type);
                await channel.BasicConsumeAsync(handlerInfo.Name, false, consumer, cancellationToken);
            }
        }
    }
}