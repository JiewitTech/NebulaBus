using Microsoft.Extensions.DependencyInjection.Extensions;
using NebulaBus;
using NebulaBus.Rabbitmq;
using System;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddNebulaBus(this IServiceCollection services, Action<NebulaOptions> setupAction)
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            var options = new NebulaOptions();
            setupAction(options);
            services.AddSingleton(options);
            services.AddSingleton<INebulaBus, NebulaBusService>();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessor, RabbitmqProcessor>());

            services.Configure(setupAction);
            services.AddHostedService<Bootstrapper>();
        }

        public static void AddNebulaBusHandler<H>(this IServiceCollection services)
            where H : NebulaHandler
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<NebulaHandler, H>());
        }

        public static void AddNebulaBusHandler<H, M>(this IServiceCollection services)
            where H : NebulaHandler<M>
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<NebulaHandler, H>());
        }
    }
}