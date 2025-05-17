using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NebulaBus.Transport.Kafka
{
    internal class KafkaTransport : ITransport
    {
        public string Name => "kafka";

        private readonly NebulaKafkaOptions _kafkaOptions;
        private readonly ILogger<KafkaTransport> _logger;
        private readonly IServiceProvider _serviceProvider;
        private bool _started;
        private readonly NebulaOptions _nebulaOptions;
        private CancellationToken _originalCancellationToken;

        public KafkaTransport(
            IServiceProvider serviceProvider,
            NebulaKafkaOptions nebulaRabbitmqOptions,
            NebulaOptions nebulaOptions,
            ILogger<KafkaTransport> logger)
        {
            _serviceProvider = serviceProvider;
            _nebulaOptions = nebulaOptions;
            _kafkaOptions = nebulaRabbitmqOptions;
            _logger = logger;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task Publish(string routingKey, object message, NebulaHeader header)
        {
            throw new NotImplementedException();
        }

        public async Task Start(CancellationToken cancellationToken)
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

            try
            {
                foreach (var info in handlerInfos)
                {
                    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Processor {nameof(KafkaTransport)} start failed");
            }
            var config = new ConsumerConfig(new Dictionary<string, string>(_kafkaOptions.ConsumerConfig));
            config.BootstrapServers ??= _kafkaOptions.Servers;
            config.GroupId ??= "nebulabus-group";
            config.AutoOffsetReset ??= AutoOffsetReset.Earliest;
            config.AllowAutoCreateTopics ??= true;
            config.EnableAutoCommit ??= false;
            config.LogConnectionClose ??= false;

            var consumer = new ConsumerBuilder<string, byte[]>(config)
                .SetErrorHandler((consumer, e) =>
                {
                    _logger.LogError($"An error occurred during connect kafka --> {e.Reason}");
                })
                .Build();

            consumer.Subscribe("");
        }

        private async Task RegisteConsumerByConfig(string name, string group, Type handlerType, CancellationToken cancellationToken)
        {
            var config = new ConsumerConfig(new Dictionary<string, string>(_kafkaOptions.ConsumerConfig));
            config.BootstrapServers ??= _kafkaOptions.Servers;
            config.GroupId ??= "nebulabus-group";
            config.AutoOffsetReset ??= AutoOffsetReset.Earliest;
            config.AllowAutoCreateTopics ??= true;
            config.EnableAutoCommit ??= false;
            config.LogConnectionClose ??= false;

            var consumer = new ConsumerBuilder<string, byte[]>(config)
                .SetErrorHandler((consumer, e) =>
                {
                    _logger.LogError($"An error occurred during connect kafka --> {e.Reason}");
                })
                .Build();
        }
    }
}
