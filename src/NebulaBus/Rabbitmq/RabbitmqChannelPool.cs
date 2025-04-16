using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaBus.Rabbitmq
{
    internal class RabbitmqChannelPool : IRabbitmqChannelPool, IDisposable
    {
        private readonly ConcurrentQueue<IChannel> _channelPool;
        private readonly SemaphoreSlim _semaphore;
        private readonly RabbitmqOptions _rabbitmqOptions;
        private readonly ILogger<RabbitmqChannelPool> _logger;
        private readonly int _maxPoolSize = 10;
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _connection;
        public RabbitmqChannelPool(NebulaOptions nebulaOptions, ILogger<RabbitmqChannelPool> logger)
        {
            _channelPool = new ConcurrentQueue<IChannel>();
            _semaphore = new SemaphoreSlim(1, 1);
            _rabbitmqOptions = nebulaOptions.RabbitmqOptions;
            _logger = logger;
            _connectionFactory = new ConnectionFactory()
            {
                UserName = _rabbitmqOptions.UserName,
                Password = _rabbitmqOptions.Password,
                VirtualHost = _rabbitmqOptions.VirtualHost,
                AutomaticRecoveryEnabled = true,
                Port = _rabbitmqOptions.Port,
                ClientProvidedName = $"NebulaBus:{Environment.MachineName}.{Assembly.GetEntryAssembly().GetName().Name}",
                TopologyRecoveryEnabled = true,
            };
            if (_rabbitmqOptions.SslOption != null)
                _connectionFactory.Ssl = _rabbitmqOptions.SslOption;
            if (!_rabbitmqOptions.HostName.Contains(","))
                _connectionFactory.HostName = _rabbitmqOptions.HostName;
        }

        private async Task<IChannel> CreateNewChannel(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_connection == null || !_connection.IsOpen)
                {
                    if (_rabbitmqOptions.HostName.Contains(","))
                        _connection = await _connectionFactory.CreateConnectionAsync(AmqpTcpEndpoint.ParseMultiple(_rabbitmqOptions.HostName), cancellationToken);
                    else _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
                }
                _connection.ConnectionShutdownAsync+= async (sender, args) =>
                {
                };
                return await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create new channel.");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default)
        {
            if (_channelPool.TryDequeue(out var channel) && channel.IsOpen)
                return channel;
            return await CreateNewChannel();
        }

        public async Task ReturnChannel(IChannel channel)
        {
            if (channel == null) return;
            if (channel.IsOpen && _channelPool.Count < _maxPoolSize)
            {
                _channelPool.Enqueue(channel);
                return;
            }
            await channel.CloseAsync();
            await channel.DisposeAsync();
        }

        public void Dispose()
        {
            while (_channelPool.TryDequeue(out var channel))
            {
                channel?.Dispose();
            }
        }
    }
}
