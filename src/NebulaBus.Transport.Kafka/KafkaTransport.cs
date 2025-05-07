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

        public Task Start(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
