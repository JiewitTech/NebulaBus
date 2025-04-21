using Microsoft.Extensions.DependencyInjection.Extensions;
using NebulaBus;
using NebulaBus.Scheduler;
using Quartz;
using Quartz.Spi;
using System;
using System.Linq;
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

            //Schedule job
            services.AddSingleton<IDelayMessageScheduler, DelayMessageScheduler>();
            services.AddKeyedSingleton<IJobFactory, NebulaBusJobFactory>("NebulaBusJobFactory");
            services.AddKeyedScoped<IJob, DelayMessageSendJob>("NebulaBusDelayMessageSendJob");

            services.Configure(setupAction);

            //Store and transport
            foreach (var provider in options.NebulaServiceProviders)
                provider.ProvideServices(services, options);

            services.AddHostedService<Bootstrapper>();
        }

        public static void AddNebulaBusHandler<TH>(this IServiceCollection services)
            where TH : NebulaHandler
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient<INebulaHandler, TH>());
            services.TryAddTransient<TH>();
        }

        public static void AddNebulaBusHandler<TH, TM>(this IServiceCollection services)
            where TH : NebulaHandler<TM>
            where TM : class, new()
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient<INebulaHandler, TH>());
            services.TryAddTransient<TH>();
        }

        public static void AddNebulaBusHandler(this IServiceCollection services, params Assembly[] assemblies)
        {
            var types = assemblies.SelectMany(x =>
                x.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(INebulaHandler).IsAssignableFrom(t)));
            foreach (var typeItem in types)
            {
                services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(INebulaHandler), typeItem));
                services.TryAddTransient(typeItem);
            }
        }

        public static void AddNebulaBusFilter<T>(this IServiceCollection services)
            where T : class, INebulaFilter
        {
            services.TryAddTransient<INebulaFilter, T>();
        }
    }
}