using System;
using NebulaBus;
using NebulaBus.Transport.Rabbitmq;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NebulaRabbitmqTransportExtension
    {
        public static void UseRabbitmqTransport(this NebulaOptions options,
            Action<NebulaRabbitmqOptions> configureOptions)
        {
            options.AddNebulaServiceProvider(new NebulaRabbitmqTransportServiceProvider(configureOptions));
        }
    }
}