using Microsoft.Extensions.Logging;
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
                HostName = _rabbitmqOptions.HostName,
                UserName = _rabbitmqOptions.UserName,
                Password = _rabbitmqOptions.Password,
                VirtualHost = _rabbitmqOptions.VirtualHost,
                AutomaticRecoveryEnabled = true,
                ClientProvidedName = $"NebulaBus:{Environment.MachineName}.{Assembly.GetEntryAssembly().GetName().Name}"
            };
        }

        private async Task<IChannel> CreateNewChannel()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_connection == null || !_connection.IsOpen)
                    _connection = await _connectionFactory.CreateConnectionAsync();
                return await _connection.CreateChannelAsync();
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

        public async Task<IChannel> GetChannelAsync()
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
