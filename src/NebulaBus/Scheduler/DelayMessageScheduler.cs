using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NebulaBus.Store;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaBus.Scheduler
{
    internal class DelayMessageScheduler : IDelayMessageScheduler
    {
        private readonly IStore _store;
        private IScheduler _scheduler;
        private readonly IJobFactory _jobFactory;
        private readonly ILogger<DelayMessageScheduler> _logger;
        private readonly JsonSerializerOptions _serializerOptions;

        public DelayMessageScheduler(IServiceProvider serviceProvider, IStore store)
        {
            _store = store;
            _jobFactory = serviceProvider.GetRequiredKeyedService<IJobFactory>("NebulaBusJobFactory");
            _logger = serviceProvider.GetRequiredService<ILogger<DelayMessageScheduler>>();
            _serializerOptions = serviceProvider.GetRequiredService<NebulaOptions>().JsonSerializerOptions;
        }

        public async Task Schedule(DelayStoreMessage delayMessage)
        {
            if (string.IsNullOrEmpty(delayMessage.MessageId))
                return;

            await _store.Add(delayMessage);
        }

        public async Task StartSchedule(CancellationToken cancellationToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.Token.Register(() =>
            {
                try
                {
                    _store?.Dispose();
                }
                catch
                {
                }
            });

            StdSchedulerFactory factory = new StdSchedulerFactory();
            _scheduler = await factory.GetScheduler(cts.Token);
            _scheduler.JobFactory = _jobFactory;
            await _scheduler.Start(cts.Token);

            while (!cts.IsCancellationRequested)
            {
                //lock
                var gotLock = _store.Lock();
                if (!gotLock)
                {
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                while (!cts.IsCancellationRequested)
                {
                    await ScheduleJobFromStore(cts.Token);
                    await Task.Delay(1000, cts.Token);
                }
            }
        }

        private IJobDetail BuildJobDetail(DelayStoreMessage delayMessage)
        {
            var job = JobBuilder.Create<DelayMessageSendJob>()
                .WithIdentity($"NebulaBusJob:{delayMessage.MessageId}.{delayMessage.Name}")
                .UsingJobData("data", JsonSerializer.Serialize(delayMessage, _serializerOptions))
                .UsingJobData("messageId", delayMessage.MessageId)
                .UsingJobData("name", delayMessage.Name)
                .UsingJobData("requestId", delayMessage.Header[NebulaHeader.RequestId])
                .Build();
            return job;
        }

        private async Task ScheduleJobFromStore(CancellationToken cancellationToken)
        {
            try
            {
                var delayMessages = await _store.Get(DateTimeOffset.Now.AddMinutes(3).ToUnixTimeSeconds());
                if (delayMessages == null) return;
                foreach (var delayMessage in delayMessages)
                {
                    if (delayMessage == null) continue;
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
                        await _scheduler.ScheduleJob(job, rightNowTrigger, cancellationToken);
                        continue;
                    }

                    var trigger = TriggerBuilder.Create()
                        .WithIdentity($"NebulaBusTrigger:{delayMessage.MessageId}.{delayMessage.Name}")
                        .StartAt(DateTimeOffset.FromUnixTimeSeconds(delayMessage.TriggerTime))
                        .Build();
                    await _scheduler.ScheduleJob(job, trigger, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ScheduleJobFromStore");
            }
        }
    }
}