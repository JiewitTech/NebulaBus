using NebulaBus;
using NebulaBus.Rabbitmq;
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

            services.AddSingleton<IStartUp, RabbitmqStartUp>();
        }
    }
}