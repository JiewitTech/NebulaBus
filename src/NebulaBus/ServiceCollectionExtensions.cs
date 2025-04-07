using Microsoft.Extensions.DependencyInjection.Extensions;
using NebulaBus;
using NebulaBus.Rabbitmq;
using NebulaBus.Scheduler;
using NebulaBus.Store;
using NebulaBus.Store.Redis;
using Quartz;
using System;
using NebulaBus.Store.Memory;
using Quartz.Simpl;
using Quartz.Spi;

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
            services.AddSingleton<IStore, RedisStore>();
            services.AddSingleton<IDelayMessageScheduler, DelayMessageScheduler>();

            //Schedule job
            services.AddKeyedSingleton<IJobFactory, NebulaBusJobFactory>("NebulaBusJobFactory");
            services.AddKeyedScoped<IJob, DelayMessageSendJob>("NebulaBusDelayMessageSendJob");

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessor, RabbitmqProcessor>());

            services.Configure(setupAction);

            //Delay Message Store
            if (!string.IsNullOrEmpty(options.RedisConnectionString))
            {
                var redisClient = new CSRedis.CSRedisClient(options.RedisConnectionString);
                services.AddSingleton(redisClient);
            }
            else
            {
                services.AddSingleton<IStore, MemoryStore>();
            }

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