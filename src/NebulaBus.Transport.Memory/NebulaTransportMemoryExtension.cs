using NebulaBus;
using NebulaBus.Transport.Memory;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NebulaTransportMemoryExtension
    {
        public static void UseMemoryTransport(this NebulaOptions nebulaOptions)
        {
            nebulaOptions.AddNebulaServiceProvider(new NebulaMemoryTransportServiceProvider());
        }
    }
}