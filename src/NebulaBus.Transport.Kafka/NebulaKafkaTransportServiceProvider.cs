using Microsoft.Extensions.DependencyInjection;

namespace NebulaBus.Transport.Kafka
{
    internal class NebulaKafkaTransportServiceProvider : INebulaServiceProvider
    {
        public void ProvideServices(IServiceCollection services, NebulaOptions options)
        {
            throw new NotImplementedException();
        }
    }
}