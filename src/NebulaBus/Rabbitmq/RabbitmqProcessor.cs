using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NebulaBus.Scheduler;
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
        private IConnection? _connection;
        private readonly List<IChannel> _channels;
        private IChannel _senderChannel;
        private readonly IDelayMessageScheduler _delayMessageScheduler;
        private readonly ILogger<RabbitmqProcessor> _logger;
        private readonly IServiceProvider _serviceProvider;
        private bool _started;
        private readonly SemaphoreSlim _semaphore;
        private readonly NebulaOptions _nebulaOptions;

        public RabbitmqProcessor(
            IServiceProvider serviceProvider,
            NebulaOptions nebulaOptions,
            IDelayMessageScheduler delayMessageScheduler,
            ILogger<RabbitmqProcessor> logger)
        {
            _serviceProvider = serviceProvider;
            _nebulaOptions = nebulaOptions;
            _rabbitmqOptions = nebulaOptions.RabbitmqOptions;
            _channels = new List<IChannel>();
            _delayMessageScheduler = delayMessageScheduler;
            _logger = logger;
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public void Dispose()
        {
            _senderChannel?.Dispose();
            foreach (var channel in _channels)
            {
                channel.CloseAsync().Wait();
                channel.Dispose();
            }

            _connection?.CloseAsync().Wait();
            _connection?.Dispose();
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            try
            {
                //Sender Channel
                _senderChannel = await CreateNewChannel();

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
            await _semaphore.WaitAsync();
            try
            {
                if (!_started)
                {
                    _logger.LogError($"Processor RabbitmqProcessor not started");
                    return;
                }

                if (_senderChannel == null || _senderChannel.IsOpen)
                    _senderChannel?.Dispose();

                _senderChannel = await CreateNewChannel();

                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(message, _nebulaOptions.JsonSerializerOptions);
                await PublishByChannel(_senderChannel, routingKey, jsonBytes, header);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Processor RabbitmqProcessor publish message to {routingKey} failed");
                throw ex;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<IConnection> GetConnection()
        {
            if (_connection == null || !_connection.IsOpen)
            {
                var connectionFactory = new ConnectionFactory()
                {
                    HostName = _rabbitmqOptions.HostName,
                    UserName = _rabbitmqOptions.UserName,
                    Password = _rabbitmqOptions.Password,
                    VirtualHost = _rabbitmqOptions.VirtualHost,
                    AutomaticRecoveryEnabled = true,
                    ClientProvidedName = $"NebulaBus:{Environment.MachineName}"
                };
                _connection = await connectionFactory.CreateConnectionAsync();
            }
            return _connection;
        }

        private async Task<IChannel> CreateNewChannel()
        {
            try
            {
                var connection = await GetConnection();
                return await connection.CreateChannelAsync();
            }
            finally
            {
            }
        }

        private async Task RegisteConsumer(CancellationToken cancellationToken)
        {
            var _nebulaHandlers = _serviceProvider.GetServices<NebulaHandler>();
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
                var channel = await CreateNewChannel();
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
                var consumer = new NebulaRabbitmqConsumer(channel, qos, async (message, header) =>
                {
                    var handler = _serviceProvider.GetService(handlerInfo.Type) as NebulaHandler;
                    if (handler == null) return;
                    await handler.Excute(_delayMessageScheduler!, message, header, _nebulaOptions.JsonSerializerOptions, (routingKey, message, header) =>
                    {
                        return PublishByChannel(channel, routingKey, message, header);
                    });
                });
                await channel.BasicConsumeAsync(handlerInfo.Name, false, consumer, cancellationToken);
            }
        }

        public async Task PublishByChannel(IChannel channel, string routingKey, ReadOnlyMemory<byte> message, NebulaHeader header)
        {
            if (!_started)
            {
                _logger.LogError($"Processor {this.GetType().Name} not started");
                return;
            }

            var props = new BasicProperties()
            {
                Headers = header.ToDictionary(x => x.Key, (x) => (object?)x.Value),
                Persistent = true,
                ContentType = "application/json"
            };

            await channel.BasicPublishAsync(_rabbitmqOptions.ExchangeName, routingKey, false, props, message);
        }
    }
}