using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NebulaBus.Transport.Rabbitmq
{
    internal class NebulaRabbitmqTransportServiceProvider : INebulaServiceProvider
    {
        private readonly Action<NebulaRabbitmqOptions> _configure;

        public NebulaRabbitmqTransportServiceProvider(Action<NebulaRabbitmqOptions> configure)
        {
            _configure = configure;
        }

        public void ProvideServices(IServiceCollection services, NebulaOptions options)
        {
            var rabbitmqOptions = new NebulaRabbitmqOptions();
            services.AddOptions<NebulaRabbitmqOptions>().Configure(_configure);
            _configure(rabbitmqOptions);
            services.AddSingleton(rabbitmqOptions);

            //Processor
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessor, RabbitmqProcessor>());

            //Rabbitmq
            services.AddSingleton<IRabbitmqChannelPool, RabbitmqChannelPool>();
        }
    }
}