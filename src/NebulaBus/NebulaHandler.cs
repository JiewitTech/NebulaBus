﻿using NebulaBus.Scheduler;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace NebulaBus
{
    public abstract class NebulaHandler
    {
        public abstract string Name { get; }
        public abstract string Group { get; }
        public virtual TimeSpan RetryInterval => TimeSpan.FromSeconds(10);
        public virtual TimeSpan RetryDelay => TimeSpan.FromSeconds(5);
        public virtual int MaxRetryCount => 10;
        public virtual byte? ExecuteThreadCount => null;

        internal abstract Task Excute(IProcessor processor, IDelayMessageScheduler delayMessageScheduler,
            string message, NebulaHeader header);

        internal abstract Task FallBackSubscribe(string message, NebulaHeader header, Exception ex);

        protected async Task DirectRetryExecute(Func<Task> operation)
        {
            for (int attempt = 1; attempt <= 4; attempt++)
            {
                try
                {
                    await operation().ConfigureAwait(false);
                    return;
                }
                catch
                {
                    if (attempt == 4) throw;
                    await Task.Delay(200);
                }
            }
        }
    }

    public abstract class NebulaHandler<T> : NebulaHandler
        where T : class, new()
    {
        internal override async Task Excute(IProcessor processor, IDelayMessageScheduler delayMessageScheduler,
            string message, NebulaHeader header)
        {
            if (string.IsNullOrEmpty(message))
            {
                await FallBackHandler(null, header, new Exception($"message is null or empty"));
                return;
            }
            header[NebulaHeader.Consumer] = Environment.MachineName;
            header[NebulaHeader.Name] = Name;
            header[NebulaHeader.Group] = Group;
            var retryCount = header.GetRetryCount();
            T data = null;
            try
            {
                if (retryCount > MaxRetryCount)
                {
                    return;
                }

                //首次执行若发生异常直接重试三次
                if (retryCount == 0)
                {
                    await DirectRetryExecute(async () =>
                    {
                        data = JsonConvert.DeserializeObject<T>(message);
                        if (data == null)
                        {
                            await FallBackHandler(data, header, new Exception($"can not deserialize from:{message}"));
                            return;
                        }

                        await Handle(data, header);
                    });
                }
                else
                {
                    data = JsonConvert.DeserializeObject<T>(message);
                    if (data == null)
                    {
                        await FallBackHandler(data, header, new Exception($"can not deserialize from:{message}"));
                        return;
                    }

                    await Handle(data, header);
                }
            }
            catch (Exception ex)
            {
                header[NebulaHeader.Exception] = ex.ToString();

                //no retry
                if (MaxRetryCount == 0)
                {
                    await FallBackHandler(data, header, new Exception($"can not deserialize from:{message}"));
                    return;
                }

                header[NebulaHeader.RetryCount] = (retryCount + 1).ToString();

                //First Time to retry，if no delay then send directly
                if (retryCount == 0 && RetryDelay.TotalSeconds <= 0)
                {
                    await processor.Publish(Name, message, header);
                    return;
                }

                //First Time to retry，if have delay then send after delay time
                if (retryCount == 0 && RetryDelay.TotalSeconds > 0)
                {
                    await delayMessageScheduler.Schedule(new Store.DelayStoreMessage()
                    {
                        Group = Group,
                        Name = Name,
                        Header = header,
                        Message = message,
                        MessageId = header[NebulaHeader.MessageId]!,
                        TriggerTime = DateTimeOffset.Now.AddSeconds(RetryDelay.TotalSeconds).ToUnixTimeSeconds()
                    });
                    return;
                }

                //out of retry count
                if (retryCount >= MaxRetryCount)
                {
                    await FallBackHandler(data, header, new Exception($"can not deserialize from:{message}"));
                    return;
                }

                //Interval Retry
                await delayMessageScheduler.Schedule(new Store.DelayStoreMessage()
                {
                    Group = Group,
                    Name = Name,
                    Header = header,
                    Message = message,
                    MessageId = header[NebulaHeader.MessageId]!,
                    TriggerTime = DateTimeOffset.Now.AddSeconds(RetryInterval.TotalSeconds).ToUnixTimeSeconds()
                });
            }
        }

        internal override async Task FallBackSubscribe(string message, NebulaHeader header, Exception ex)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<T>(message);
                if (data == null) return;
                await FallBackHandler(data, header, ex);
            }
            catch
            {
            }
        }

        protected abstract Task Handle(T message, NebulaHeader header);

        protected virtual async Task FallBackHandler(T handler, NebulaHeader header, Exception exception)
        {
            await Task.CompletedTask;
        }
    }
}