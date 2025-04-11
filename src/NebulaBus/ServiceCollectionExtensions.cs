using FreeRedis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NebulaBus;
using NebulaBus.Rabbitmq;
using NebulaBus.Scheduler;
using NebulaBus.Store;
using NebulaBus.Store.Memory;
using NebulaBus.Store.Redis;
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

            //Processor
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessor, RabbitmqProcessor>());

            //Schedule job
            services.AddSingleton<IDelayMessageScheduler, DelayMessageScheduler>();
            services.AddKeyedSingleton<IJobFactory, NebulaBusJobFactory>("NebulaBusJobFactory");
            services.AddKeyedScoped<IJob, DelayMessageSendJob>("NebulaBusDelayMessageSendJob");

            services.Configure(setupAction);

            //Delay Message Store
            if (!string.IsNullOrEmpty(options.RedisConnectionString))
            {
                var freeRedisClient = new RedisClient(options.RedisConnectionString);
                services.AddKeyedSingleton("NebulaBusRedis", freeRedisClient);
                services.AddSingleton<IStore, RedisStore>();
            }
            else
            {
                services.AddSingleton<IStore, MemoryStore>();
            }

            services.AddHostedService<Bootstrapper>();
        }

        public static void AddNebulaBusHandler<TH>(this IServiceCollection services)
            where TH : NebulaHandler
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<NebulaHandler, TH>());
        }

        public static void AddNebulaBusHandler<TH, TM>(this IServiceCollection services)
            where TH : NebulaHandler<TM>
            where TM : class, new()
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<NebulaHandler, TH>());
        }

        public static void AddNebulaBusHandler(this IServiceCollection services, Assembly assembly)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(NebulaHandler).IsAssignableFrom(t));
            foreach (var typeItem in types)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(NebulaHandler), typeItem));
            }
        }
    }
}