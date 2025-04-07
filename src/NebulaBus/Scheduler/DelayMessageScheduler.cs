using NebulaBus.Store;
using Quartz;
using Quartz.Impl;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NebulaBus.Scheduler
{
    internal class DelayMessageScheduler : IDelayMessageScheduler
    {
        private readonly IStore _store;
        private IScheduler _senderScheduler;
        private IScheduler _scheduler;

        public DelayMessageScheduler(IStore store)
        {
            _store = store;
        }

        public async Task Schedule(DelayStoreMessage delayMessage)
        {
            if (string.IsNullOrEmpty(delayMessage.MessageId))
            {
                return;
            }
            var job = JobBuilder.Create<DelayMessageSendJob>()
                .WithIdentity($"Schedule:{delayMessage.MessageId}")
                .UsingJobData("data", JsonConvert.SerializeObject(delayMessage))
                .Build();

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
            await _scheduler.Start();

            var delayMessages = await _store.GetAll();
            foreach (var delayMessage in delayMessages)
            {
                var job = JobBuilder.Create<DelayMessageSendJob>()
                    .WithIdentity($"Schedule:{delayMessage.Key}")
                    .UsingJobData("data", JsonConvert.SerializeObject(delayMessage))
                    .Build();

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
            await _senderScheduler.Start();
        }
    }
}