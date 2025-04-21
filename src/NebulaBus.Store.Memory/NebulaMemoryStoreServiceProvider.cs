using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NebulaBus.Store.Memory
{
    internal class NebulaMemoryStoreServiceProvider : INebulaServiceProvider
    {
        public void ProvideServices(IServiceCollection services, NebulaOptions options)
        {
            services.TryAddSingleton<IStore, MemoryStore>();
        }
    }
}