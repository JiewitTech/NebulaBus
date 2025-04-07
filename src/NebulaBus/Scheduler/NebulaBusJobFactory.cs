using System;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;

namespace NebulaBus.Scheduler
{
    internal class NebulaBusJobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public NebulaBusJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return _serviceProvider.GetRequiredKeyedService<IJob>("NebulaBusDelayMessageSendJob");
        }

        public void ReturnJob(IJob job)
        {
            (job as IDisposable)?.Dispose();
        }
    }
}