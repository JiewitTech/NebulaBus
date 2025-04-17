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

        public void Schedule(DelayStoreMessage delayMessage)
        {
            if (string.IsNullOrEmpty(delayMessage.MessageId))
                return;

            _store.Add(delayMessage);
        }

        public async Task StartSchedule(CancellationToken cancellationToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            StdSchedulerFactory factory = new StdSchedulerFactory();
            _scheduler = await factory.GetScheduler(cts.Token);
            _scheduler.JobFactory = _jobFactory;
            await _scheduler.Start(cts.Token);

            var timer = new System.Timers.Timer(1000);
            timer.Elapsed += Timer_Elapsed;
            var lockValue = Guid.NewGuid().ToString();

            cts.Token.Register(() =>
            {
                _scheduler.Clear();
                _scheduler.Shutdown();
                timer?.Stop();
                timer?.Dispose();
                _store?.UnLock(lockValue);
                _store?.Dispose();
            });

            while (!cts.IsCancellationRequested)
            {
                try
                {
                    //lock
                    var gotLock = _store.Lock(lockValue);
                    if (!gotLock)
                    {
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }
                    timer.Start();
                    while (!cts.IsCancellationRequested)
                    {
                        await ScheduleJobFromStore(cts.Token);
                        await Task.Delay(1000, cts.Token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Schedule Failed");
                    await Task.Delay(1000, cts.Token);
                }
            }
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _store.RefreshLock();
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
    }
}