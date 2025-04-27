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

        public void Schedule(NebulaStoreMessage nebulaMessage)
        {
            if (string.IsNullOrEmpty(nebulaMessage.MessageId))
                return;

            _store.Add(nebulaMessage);
        }

        public async Task StartSchedule(CancellationToken cancellationToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _scheduler = await SchedulerBuilder.Create(Guid.NewGuid().ToString(), "NebulaBusScheduler").BuildScheduler();
            _scheduler.JobFactory = _jobFactory;
            await _scheduler.Start(cts.Token);

            var timer = new System.Timers.Timer(1000);
            timer.Elapsed += Timer_Elapsed;
            var lockValue = Guid.NewGuid().ToString();

            _store.Init();

            cts.Token.Register(() =>
            {
                _scheduler.Clear();
                _scheduler.Shutdown();
                timer?.Stop();
                timer?.Dispose();
                _store?.UnLock(lockValue);
                _store?.Dispose();
                _logger.LogInformation($"MessageScheduler Shutdown!");
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
                        await Task.Delay(1000);
                    }
                }
                catch (Exception ex)
                {
                    timer.Stop();
                    _logger.LogError(ex, "Schedule Failed");
                }
                await Task.Delay(1000);
            }

            _logger.LogInformation($"MessageScheduler Shutdown!");
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _store.RefreshLock();
        }

        private IJobDetail BuildJobDetail(NebulaStoreMessage nebulaMessage)
        {
            var job = JobBuilder.Create<DelayMessageSendJob>()
                .WithIdentity($"NebulaBusJob:{nebulaMessage.GetKey()}")
                .UsingJobData("data", JsonSerializer.Serialize(nebulaMessage, _serializerOptions))
                .UsingJobData("messageId", nebulaMessage.MessageId)
                .UsingJobData("name", nebulaMessage.Name)
                .UsingJobData("requestId", nebulaMessage.Header[NebulaHeader.RequestId])
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
                        .WithIdentity($"NebulaBusTrigger:{delayMessage.GetKey()}")
                        .StartNow()
                        .Build();
                    await _scheduler.ScheduleJob(job, rightNowTrigger, cancellationToken);
                    continue;
                }

                var trigger = TriggerBuilder.Create()
                    .WithIdentity($"NebulaBusTrigger:{delayMessage.GetKey()}")
                    .StartAt(DateTimeOffset.FromUnixTimeSeconds(delayMessage.TriggerTime))
                    .Build();
                await _scheduler.ScheduleJob(job, trigger, cancellationToken);
            }
        }
    }
}