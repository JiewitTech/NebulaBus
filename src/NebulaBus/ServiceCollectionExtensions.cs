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
using System.Text.Json;

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

            //Rabbitmq
            services.AddSingleton<IRabbitmqChannelPool, RabbitmqChannelPool>();

            //Schedule job
            services.AddSingleton<IDelayMessageScheduler, DelayMessageScheduler>();
            services.AddKeyedSingleton<IJobFactory, NebulaBusJobFactory>("NebulaBusJobFactory");
            services.AddKeyedScoped<IJob, DelayMessageSendJob>("NebulaBusDelayMessageSendJob");

            services.Configure(setupAction);

            //Delay Message Store
            if (!string.IsNullOrEmpty(options.RedisConnectionString))
            {
                var freeRedisClient = new RedisClient(options.RedisConnectionString);
                freeRedisClient.Serialize = obj => JsonSerializer.Serialize(obj, options.JsonSerializerOptions);
                freeRedisClient.Deserialize = (json, type) => JsonSerializer.Deserialize(json, type, options.JsonSerializerOptions);
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
            var types = assemblies.SelectMany(x => x.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(INebulaHandler).IsAssignableFrom(t)));
            foreach (var typeItem in types)
            {
                services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(INebulaHandler), typeItem));
                services.TryAddTransient(typeItem);
            }
        }
    }
}