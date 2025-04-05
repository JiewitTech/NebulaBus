using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading.Tasks;

namespace NebulaBus.Rabbitmq
{
    public class RabbitmqStartUp : IStartUp
    {
        private readonly RabbitmqOptions _rabbitmqOptions;
        private readonly NebulaHandler[] _nebulaHandlers;
        private IConnection? _connection;
        public RabbitmqStartUp(RabbitmqOptions rabbitmqOptions, NebulaHandler[] nebulaHandlers)
        {
            _rabbitmqOptions = rabbitmqOptions;
            _nebulaHandlers = nebulaHandlers;
        }

        public async Task Start()
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.UserName = _rabbitmqOptions.UserName;
            factory.Password = _rabbitmqOptions.Password;
            factory.VirtualHost = _rabbitmqOptions.VirtualHost;
            factory.HostName = _rabbitmqOptions.HostName;
            factory.AutomaticRecoveryEnabled = true;
            factory.ClientProvidedName = $"NebulaBus:{Environment.MachineName}";

            _connection = await factory.CreateConnectionAsync();

            foreach (var handler in _nebulaHandlers)
            {
                using var channel = await _connection.CreateChannelAsync();
                await channel.ExchangeDeclareAsync(handler.Group, ExchangeType.Topic);
                await channel.QueueDeclareAsync(handler.Name, false, false, false, null);
                await channel.QueueBindAsync(handler.Name, _rabbitmqOptions.ExchangeName, handler.Group, null);
                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += async (ch, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var header = new NebulaHeader();
                    foreach (var item in ea.BasicProperties.Headers!)
                        header.Add(item.Key, item.Value!.ToString());
                    await handler.Subscribe(message, header);
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                };
            }
        }
    }
}