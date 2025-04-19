using Microsoft.Extensions.DependencyInjection;
using NebulaBus.Scheduler;
using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace NebulaBus
{
    internal interface INebulaHandler
    {
        public string Name { get; }
        public string Group { get; }
        public TimeSpan RetryInterval { get; }
        public TimeSpan RetryDelay { get; }
        public int MaxRetryCount { get; }
        public byte? ExecuteThreadCount { get; }
    }

    public abstract class NebulaHandler : INebulaHandler
    {
        public abstract string Name { get; }
        public abstract string Group { get; }
        public virtual TimeSpan RetryInterval => TimeSpan.FromSeconds(10);
        public virtual TimeSpan RetryDelay => TimeSpan.FromSeconds(5);
        public virtual int MaxRetryCount => 10;
        public virtual byte? ExecuteThreadCount => null;

        internal abstract Task Excute(
            IServiceProvider serviceProvider,
            ReadOnlyMemory<byte> message,
            NebulaHeader header);

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
            IServiceProvider serviceProvider,
            ReadOnlyMemory<byte> message,
            NebulaHeader header)
        {
            var delayMessageScheduler = serviceProvider.GetRequiredService<IDelayMessageScheduler>();
            var jsonSerializerOptions = serviceProvider.GetRequiredService<NebulaOptions>().JsonSerializerOptions;
            var filter = serviceProvider.GetService<INebulaFilter>();

            (bool success, T? data, Exception? exception) = await DeSerializer(message, header, jsonSerializerOptions);
            if (!success || data == null)
            {
                var exp = new Exception($"DeSerializer message failed", exception);
                await NebulaExtension.ExcuteHandlerWithoutException(() => FallBackHandle(data, header, exp),
                    () => filter?.FallBackHandle(data, header, exp));
                return;
            }

            if (message.IsEmpty)
            {
                var exp = new Exception($"message is null or empty");
                await NebulaExtension.ExcuteHandlerWithoutException(() => FallBackHandle(data, header, exp),
                    () => filter?.FallBackHandle(data, header, exp));
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

                var res = await NebulaExtension.ExcuteBeforeHandlerWithoutException(() => BeforeHandle(data, header),
                    () => filter?.BeforeHandle(data, header));
                if (!res) return;
                //首次执行若发生异常直接重试三次
                if (retryCount == 0)
                {
                    await DirectRetryExecute(async () => { await Handle(data, header); });
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
                    await NebulaExtension.ExcuteHandlerWithoutException(() => FallBackHandle(data, header, ex),
                        () => filter?.FallBackHandle(data, header, ex));
                    return;
                }

                header[NebulaHeader.RetryCount] = (retryCount + 1).ToString();

                //First Time to retry，use retry delay
                if (retryCount == 0)
                {
                    delayMessageScheduler.Schedule(new Store.NebulaStoreMessage()
                    {
                        Group = Group,
                        Name = Name,
                        Header = header,
                        Message = data,
                        MessageId = header.GetMessageId(),
                        TriggerTime = DateTimeOffset.Now.AddSeconds(RetryDelay.TotalSeconds).ToUnixTimeSeconds(),
                        Transport = header[NebulaHeader.Transport]
                    });
                    return;
                }

                //out of retry count
                if (retryCount >= MaxRetryCount)
                {
                    await NebulaExtension.ExcuteHandlerWithoutException(() => FallBackHandle(data, header, ex),
                        () => filter?.FallBackHandle(data, header, ex));
                    return;
                }

                //Interval Retry
                delayMessageScheduler.Schedule(new Store.NebulaStoreMessage()
                {
                    Group = Group,
                    Name = Name,
                    Header = header,
                    Message = data,
                    MessageId = header.GetMessageId(),
                    TriggerTime = DateTimeOffset.Now.AddSeconds(RetryInterval.TotalSeconds).ToUnixTimeSeconds(),
                    Transport = header[NebulaHeader.Transport]
                });
            }
            finally
            {
                await NebulaExtension.ExcuteHandlerWithoutException(() => AfterHandle(data, header),
                    () => filter?.AfterHandle(data, header));
            }
        }

        protected abstract Task Handle(T message, NebulaHeader header);

        protected virtual Task FallBackHandle(T? message, NebulaHeader header, Exception exception)
        {
            throw new NotImplementedException();
        }

        protected virtual Task<bool> BeforeHandle(T? message, NebulaHeader header)
        {
            throw new NotImplementedException();
        }

        protected virtual Task AfterHandle(T? message, NebulaHeader header)
        {
            throw new NotImplementedException();
        }

        private async Task<(bool success, T? data, Exception? ex)> DeSerializer(ReadOnlyMemory<byte> message,
            NebulaHeader header, JsonSerializerOptions jsonSerializerOptions)
        {
            try
            {
                T data = JsonSerializer.Deserialize<T>(message.Span, jsonSerializerOptions)!;
                return (true, data, null);
            }
            catch (Exception ex)
            {
                await FallBackHandle(new T(), header, ex);
                return (false, null, ex);
            }
        }
    }
}