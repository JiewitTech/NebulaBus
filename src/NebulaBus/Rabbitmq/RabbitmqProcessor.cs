using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NebulaBus.Scheduler;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
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

        public RabbitmqProcessor(
            IServiceProvider serviceProvider,
            NebulaOptions nebulaOptions,
            IDelayMessageScheduler delayMessageScheduler,
            ILogger<RabbitmqProcessor> logger)
        {
            _serviceProvider = serviceProvider;
            _rabbitmqOptions = nebulaOptions.RabbitmqOptions;
            _channels = new List<IChannel>();
            _delayMessageScheduler = delayMessageScheduler;
            _logger = logger;
        }

        public void Dispose()
        {
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
            ConnectionFactory factory = new ConnectionFactory();
            factory.UserName = _rabbitmqOptions.UserName;
            factory.Password = _rabbitmqOptions.Password;
            factory.VirtualHost = _rabbitmqOptions.VirtualHost;
            factory.HostName = _rabbitmqOptions.HostName;
            factory.AutomaticRecoveryEnabled = true;
            factory.ClientProvidedName = $"NebulaBus:{Environment.MachineName}";

            _connection = await factory.CreateConnectionAsync();

            //Sender Channel
            _senderChannel = await _connection.CreateChannelAsync();
            _channels.Add(_senderChannel);

            var _nebulaHandlers = _serviceProvider.GetServices<NebulaHandler>();
            if (_nebulaHandlers != null)
            {
                foreach (var handler in _nebulaHandlers)
                {
                    var channel = await _connection.CreateChannelAsync();
                    var getQos = _rabbitmqOptions.GetQos?.Invoke(handler.Name, handler.Group);
                    var qos = getQos > 0 ? getQos : _rabbitmqOptions.Qos;
                    if (qos > 0)
                        await channel.BasicQosAsync(0, qos.Value, false);

                    //Create Exchange
                    await channel.ExchangeDeclareAsync(_rabbitmqOptions.ExchangeName, ExchangeType.Direct, true);
                    //Create Queue
                    await channel.QueueDeclareAsync(handler.Name, true, false, false, null);

                    //Bind Group RoutingKey
                    if (!string.IsNullOrEmpty(handler.Group))
                        await channel.QueueBindAsync(handler.Name, _rabbitmqOptions.ExchangeName, handler.Group, null);

                    //Bind Name RoutingKey
                    if (!string.IsNullOrEmpty(handler.Name))
                        await channel.QueueBindAsync(handler.Name, _rabbitmqOptions.ExchangeName, handler.Name, null);

                    _channels.Add(channel);
                    var consumer = new AsyncEventingBasicConsumer(channel);
                    await channel.BasicConsumeAsync(handler.Name, false, consumer, cancellationToken);

                    consumer.ReceivedAsync += async (ch, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var header = new NebulaHeader();
                        if (ea.BasicProperties.Headers != null)
                        {
                            foreach (var item in ea.BasicProperties.Headers!)
                            {
                                if (item.Value is byte[] bytes) header.Add(item.Key, Encoding.UTF8.GetString(bytes));
                            }
                        }

                        await handler.Subscribe(this, _delayMessageScheduler, message, header);
                        await channel.BasicAckAsync(ea.DeliveryTag, false);
                    };
                }
            }

            _started = true;
        }

        public async Task Publish(string routingKey, string message, NebulaHeader header)
        {
            if (!_started)
            {
                _logger.LogError($"Processor {this.GetType().Name} not started");
                return;
            }

            byte[] messageBodyBytes = Encoding.UTF8.GetBytes(message);
            var props = new BasicProperties()
            {
                Headers = new Dictionary<string, object?>()
            };
            props.Persistent = true;
            foreach (var item in header)
                props.Headers.Add(item.Key, item.Value);

            await _senderChannel.BasicPublishAsync(_rabbitmqOptions.ExchangeName, routingKey, false, props,
                messageBodyBytes);
        }
    }
}