using Confluent.Kafka;

namespace NebulaBus.Transport.Kafka
{
    public class NebulaKafkaOptions
    {
        public IList<ErrorCode> RetriableErrorCodes { get; set; }
        public string Servers { get; set; }
        public IDictionary<string, string> ConsumerConfig { get; set; }
    }
}
