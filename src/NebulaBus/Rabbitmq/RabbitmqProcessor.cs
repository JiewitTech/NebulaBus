using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NebulaBus.Rabbitmq
{
    public class RabbitmqProcessor : IProcessor
    {
        private readonly RabbitmqOptions _rabbitmqOptions;
        private readonly IEnumerable<NebulaHandler> _nebulaHandlers;
        private IConnection? _connection;
        private readonly List<IChannel> _channels;
        private IChannel _senderChannel;
        internal const string DelayQueue = "NebulaBus.DelayQueue";
        internal const string DelayRoutingKey = "NebulaBus.DelayRoutingKey";
        public RabbitmqProcessor(NebulaOptions nebulaOptions, IEnumerable<NebulaHandler> nebulaHandlers)
        {
            _rabbitmqOptions = nebulaOptions.RabbitmqOptions;
            _nebulaHandlers = nebulaHandlers;
            _channels = new List<IChannel>();
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

            foreach (var handler in _nebulaHandlers)
            {
                var channel = await _connection.CreateChannelAsync();
                await channel.ExchangeDeclareAsync(_rabbitmqOptions.ExchangeName, ExchangeType.Direct);
                await channel.QueueDeclareAsync(handler.Name, false, false, false, null);
                await channel.QueueBindAsync(handler.Name, _rabbitmqOptions.ExchangeName, handler.Group, null);
                _channels.Add(channel);
                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (ch, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var header = new NebulaHeader();
                    foreach (var item in ea.BasicProperties.Headers!)
                        header.Add(item.Key, item.Value!.ToString());
                    try
                    {
                        await handler.Subscribe(message, header);
                    }
                    catch (Exception ex)
                    {
                        header[NebulaHeader.Exception] = ex.ToString();
                        int.TryParse(header[NebulaHeader.RetryCount], out var retryCount);
                        if (retryCount > handler.MaxRetryCount) return;
                        header[NebulaHeader.RetryCount] = (retryCount + 1).ToString();

                        //First Time to retry
                        if (handler.RetryDelay.TotalSeconds <= 0 && retryCount == 0)
                        {
                            await Send(handler.Group, message, header);
                            return;
                        }

                        //Interval Retry
                        await Send(DelayRoutingKey, message, header);
                    }
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                };
            }
        }

        public async Task Send(string group, string message, NebulaHeader header)
        {
            byte[] messageBodyBytes = Encoding.UTF8.GetBytes("Hello, world!");
            var props = new BasicProperties();
            await _senderChannel.BasicPublishAsync(_rabbitmqOptions.ExchangeName, group, false, props, messageBodyBytes);
        }

        public async Task SendDelay(string group, string message, NebulaHeader header, TimeSpan delay)
        {
            byte[] messageBodyBytes = Encoding.UTF8.GetBytes("Hello, world!");
            var props = new BasicProperties();
            await _senderChannel.BasicPublishAsync(_rabbitmqOptions.ExchangeName, group, false, props, messageBodyBytes);
        }
    }
}