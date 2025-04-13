using NebulaBus.Scheduler;
using System;
using System.Reflection;
using System.Text.Json;
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

        internal abstract Task Excute(
            IDelayMessageScheduler delayMessageScheduler,
            ReadOnlyMemory<byte> message,
            NebulaHeader header,
            JsonSerializerOptions jsonSerializerOptions,
            Func<string, ReadOnlyMemory<byte>, NebulaHeader, Task> repulish);

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
                    await Task.Delay(50);
                }
            }
        }
    }

    public abstract class NebulaHandler<T> : NebulaHandler
        where T : class, new()
    {
        internal override async Task Excute(
            IDelayMessageScheduler delayMessageScheduler,
            ReadOnlyMemory<byte> message,
            NebulaHeader header,
            JsonSerializerOptions jsonSerializerOptions,
            Func<string, ReadOnlyMemory<byte>, NebulaHeader, Task> repulish)
        {
            (bool success, T? data, Exception? exception) = await DeSerializer(message, header, jsonSerializerOptions);
            if (!success || data == null)
            {
                await FallBackHandler(data, header, new Exception($"DeSerializer message failed", exception));
                return;
            }
            if (message.IsEmpty)
            {
                await FallBackHandler(data, header, new Exception($"message is null or empty"));
                return;
            }
            header[NebulaHeader.Consumer] = $"{Environment.MachineName}.{Assembly.GetEntryAssembly().GetName().Name}";
            header[NebulaHeader.Name] = Name;
            header[NebulaHeader.Group] = Group;
            var retryCount = header.GetRetryCount();
            try
            {
                if (retryCount > MaxRetryCount)
                    return;

                //首次执行若发生异常直接重试三次
                if (retryCount == 0)
                {
                    await DirectRetryExecute(async () =>
                    {
                        await Handle(data, header);
                    });
                }
                else
                {
                    await Handle(data, header);
                }
            }
            catch (Exception ex)
            {
                header[NebulaHeader.Exception] = ex.ToString();

                //no retry
                if (MaxRetryCount == 0)
                {
                    await FallBackHandler(data, header, ex);
                    return;
                }

                header[NebulaHeader.RetryCount] = (retryCount + 1).ToString();

                //First Time to retry，if no delay then send directly
                if (retryCount == 0 && RetryDelay.TotalSeconds <= 0)
                {
                    await repulish(Name, message, header);
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
                        Message = data,
                        MessageId = header[NebulaHeader.MessageId]!,
                        TriggerTime = DateTimeOffset.Now.AddSeconds(RetryDelay.TotalSeconds).ToUnixTimeSeconds()
                    });
                    return;
                }

                //out of retry count
                if (retryCount >= MaxRetryCount)
                {
                    await FallBackHandler(data, header, ex);
                    return;
                }

                //Interval Retry
                await delayMessageScheduler.Schedule(new Store.DelayStoreMessage()
                {
                    Group = Group,
                    Name = Name,
                    Header = header,
                    Message = data,
                    MessageId = header[NebulaHeader.MessageId]!,
                    TriggerTime = DateTimeOffset.Now.AddSeconds(RetryInterval.TotalSeconds).ToUnixTimeSeconds()
                });
            }
        }

        protected abstract Task Handle(T message, NebulaHeader header);

        protected virtual async Task FallBackHandler(T? message, NebulaHeader header, Exception exception)
        {
            await Task.CompletedTask;
        }

        private async Task<(bool success, T? data, Exception? ex)> DeSerializer(ReadOnlyMemory<byte> message, NebulaHeader header, JsonSerializerOptions jsonSerializerOptions)
        {
            try
            {
                T data = JsonSerializer.Deserialize<T>(message.Span, jsonSerializerOptions)!;
                return (true, data, null);
            }
            catch (Exception ex)
            {
                await FallBackHandler(new T(), header, ex);
                return (false, null, ex);
            }
        }
    }
}