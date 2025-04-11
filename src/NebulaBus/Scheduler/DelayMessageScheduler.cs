using NebulaBus.Store;
using Quartz;
using Quartz.Impl;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quartz.Spi;

namespace NebulaBus.Scheduler
{
    internal class DelayMessageScheduler : IDelayMessageScheduler
    {
        private readonly IStore _store;
        private IScheduler _scheduler;
        private readonly IJobFactory _jobFactory;
        private readonly ILogger<DelayMessageScheduler> _logger;

        public DelayMessageScheduler(IServiceProvider serviceProvider, IStore store)
        {
            _store = store;
            _jobFactory = serviceProvider.GetRequiredKeyedService<IJobFactory>("NebulaBusJobFactory");
            _logger = serviceProvider.GetRequiredService<ILogger<DelayMessageScheduler>>();
        }

        public async Task Schedule(DelayStoreMessage delayMessage)
        {
            if (string.IsNullOrEmpty(delayMessage.MessageId))
                return;

            await _store.Add(delayMessage);
        }

        public async Task StartSchedule(CancellationToken cancellationToken)
        {
            StdSchedulerFactory factory = new StdSchedulerFactory();
            _scheduler = await factory.GetScheduler();
            _scheduler.JobFactory = _jobFactory;
            await _scheduler.Start();

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Cancelling loop lock scheduler");
                    return;
                }

                //lock
                var gotLock = _store.Lock();
                if (!gotLock)
                {
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Cancelling loop scheduler");
                        return;
                    }

                    await ScheduleJobFromStore(cancellationToken);
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        private static IJobDetail BuildJobDetail(DelayStoreMessage delayMessage)
        {
            var job = JobBuilder.Create<DelayMessageSendJob>()
                .WithIdentity($"NebulaBusJob:{delayMessage.MessageId}.{delayMessage.Name}")
                .UsingJobData("data", JsonConvert.SerializeObject(delayMessage))
                .UsingJobData("messageId", delayMessage.MessageId)
                .UsingJobData("name", delayMessage.Name)
                .UsingJobData("requestId", delayMessage.Header[NebulaHeader.RequestId])
                .Build();
            return job;
        }

        private async Task ScheduleJobFromStore(CancellationToken cancellationToken)
        {
            var delayMessages = await _store.GetAllByKeys(DateTimeOffset.Now.AddMinutes(1).ToUnixTimeSeconds());
            if (delayMessages == null) return;
            foreach (var delayMessage in delayMessages)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                var job = BuildJobDetail(delayMessage);
                if (await _scheduler.CheckExists(job.Key, cancellationToken))
                    continue;
                if (delayMessage.TriggerTime < DateTimeOffset.Now.ToUnixTimeSeconds())
                {
                    var rightNowTrigger = TriggerBuilder.Create()
                        .WithIdentity($"NebulaBusTrigger:{delayMessage.MessageId}.{delayMessage.Name}")
                        .StartNow()
                        .Build();
                    await _scheduler.ScheduleJob(job, rightNowTrigger);
                    continue;
                }

                var trigger = TriggerBuilder.Create()
                    .WithIdentity($"NebulaBusTrigger:{delayMessage.MessageId}.{delayMessage.Name}")
                    .StartAt(DateTimeOffset.FromUnixTimeSeconds(delayMessage.TriggerTime))
                    .Build();
                await _scheduler.ScheduleJob(job, trigger);
            }
        }
    }
}