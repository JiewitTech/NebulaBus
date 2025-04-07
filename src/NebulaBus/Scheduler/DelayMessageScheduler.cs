using NebulaBus.Store;
using Quartz;
using Quartz.Impl;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Quartz.Spi;

namespace NebulaBus.Scheduler
{
    internal class DelayMessageScheduler : IDelayMessageScheduler
    {
        private readonly IStore _store;
        private IScheduler _senderScheduler;
        private IScheduler _scheduler;
        private readonly IJobFactory _jobFactory;

        public DelayMessageScheduler(IServiceProvider serviceProvider, IStore store)
        {
            _store = store;
            _jobFactory = serviceProvider.GetRequiredKeyedService<IJobFactory>("NebulaBusJobFactory");
        }

        public async Task Schedule(DelayStoreMessage delayMessage)
        {
            if (string.IsNullOrEmpty(delayMessage.MessageId))
            {
                return;
            }

            var job = BuildJobDetail(delayMessage);

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"Delay:{delayMessage.MessageId}")
                .StartAt(delayMessage.TriggerTime)
                .Build();

            await _store.Add(delayMessage);
            await _senderScheduler.ScheduleJob(job, trigger);
        }

        public async Task StartStoreSchedule()
        {
            StdSchedulerFactory factory = new StdSchedulerFactory();
            _scheduler = await factory.GetScheduler();
            _scheduler.JobFactory = _jobFactory;
            await _scheduler.Start();

            var delayMessages = await _store.GetAll();
            foreach (var delayMessage in delayMessages)
            {
                var job = BuildJobDetail(delayMessage.Value);

                if (delayMessage.Value.TriggerTime < DateTimeOffset.Now)
                {
                    var rightNowTrigger = TriggerBuilder.Create()
                        .WithIdentity($"Delay:{delayMessage.Key}")
                        .StartNow()
                        .Build();
                    await _scheduler.ScheduleJob(job, rightNowTrigger);
                    continue;
                }

                var trigger = TriggerBuilder.Create()
                    .WithIdentity($"Delay:{delayMessage.Key}")
                    .StartAt(delayMessage.Value.TriggerTime)
                    .Build();
                await _scheduler.ScheduleJob(job, trigger);
            }
        }

        public async Task StartSenderScheduler()
        {
            StdSchedulerFactory factory = new StdSchedulerFactory();
            _senderScheduler = await factory.GetScheduler();
            _senderScheduler.JobFactory = _jobFactory;
            await _senderScheduler.Start();
        }

        private static IJobDetail BuildJobDetail(DelayStoreMessage delayMessage)
        {
            var job = JobBuilder.Create<DelayMessageSendJob>()
                .WithIdentity($"Schedule:{delayMessage.MessageId}")
                .UsingJobData("data", JsonConvert.SerializeObject(delayMessage))
                .UsingJobData("messageId", delayMessage.MessageId)
                .UsingJobData("name", delayMessage.Name)
                .UsingJobData("requestId", delayMessage.Header[NebulaHeader.RequestId])
                .Build();
            return job;
        }
    }
}