﻿using Microsoft.Extensions.DependencyInjection.Extensions;
using NebulaBus;
using NebulaBus.Rabbitmq;
using NebulaBus.Scheduler;
using NebulaBus.Store;
using NebulaBus.Store.Redis;
using Quartz;
using System;

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
            services.AddScoped<IJob, DelayMessageSendJob>();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IProcessor, RabbitmqProcessor>());

            services.Configure(setupAction);

            if (!string.IsNullOrEmpty(options.RedisConnectionString))
            {
                var redisClient = new CSRedis.CSRedisClient(options.RedisConnectionString);
                services.AddSingleton(redisClient);
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