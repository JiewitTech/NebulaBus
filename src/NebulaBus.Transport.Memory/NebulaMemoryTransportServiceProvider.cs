using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NebulaBus.Transport.Memory
{
    internal class NebulaMemoryTransportServiceProvider : INebulaServiceProvider
    {
        public void ProvideServices(IServiceCollection services, NebulaOptions options)
        {
            //Processor
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessor, MemoryProcessor>());
        }
    }
}