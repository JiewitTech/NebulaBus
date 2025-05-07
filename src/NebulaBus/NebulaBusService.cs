using Microsoft.Extensions.DependencyInjection;
using NebulaBus.Scheduler;
using NebulaBus.Store;
using Snowflake.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace NebulaBus
{
    internal class NebulaBusService : INebulaBus
    {
        private readonly IEnumerable<ITransport> _processors;
        private readonly IDelayMessageScheduler _delayMessageScheduler;
        private readonly IdWorker _idWorker;

        public NebulaBusService(IServiceProvider serviceProvider)
        {
            _processors = serviceProvider.GetRequiredService<IEnumerable<ITransport>>();
            _delayMessageScheduler = serviceProvider.GetRequiredService<IDelayMessageScheduler>();
            if (!long.TryParse(Environment.GetEnvironmentVariable("NEBULABUS_WORKERID"), out var result))
                result = 1;
            _idWorker = new IdWorker(result, 1);
        }

        public async Task PublishAsync<T>(string nameOrGroup, T message)
            where T : class, new()
        {
            var header = BuildNebulaHeader<T>(nameOrGroup);
            foreach (var processor in _processors)
            {
                header[NebulaHeader.Transport] = processor.Name;
                await processor.Publish(nameOrGroup, message, header);
            }
        }

        public async Task PublishAsync<T>(string nameOrGroup, T message, IDictionary<string, string> headers)
            where T : class, new()
        {
            var header = BuildNebulaHeader<T>(nameOrGroup, headers);
            foreach (var processor in _processors)
            {
                header[NebulaHeader.Transport] = processor.Name;
                await processor.Publish(nameOrGroup, message, header);
            }
        }

        public async Task PublishAsync<T>(TimeSpan delay, string nameOrGroup, T message)
            where T : class, new()
        {
            var header = BuildNebulaHeader<T>(nameOrGroup);
            await Task.Run(() =>
            {
                _delayMessageScheduler.Schedule(new NebulaStoreMessage()
                {
                    MessageId = header[NebulaHeader.MessageId]!,
                    Group = nameOrGroup,
                    Header = header,
                    Message = message,
                    Name = nameOrGroup,
                    TriggerTime = DateTimeOffset.Now.AddSeconds(delay.TotalSeconds).ToUnixTimeSeconds()
                });
            });
        }

        public async Task PublishAsync<T>(TimeSpan delay, string nameOrGroup, T message, IDictionary<string, string> headers)
            where T : class, new()
        {
            var header = BuildNebulaHeader<T>(nameOrGroup, headers);
            await Task.Run(() =>
            {
                _delayMessageScheduler.Schedule(new NebulaStoreMessage()
                {
                    MessageId = header[NebulaHeader.MessageId]!,
                    Group = nameOrGroup,
                    Header = header,
                    Message = message,
                    Name = nameOrGroup,
                    TriggerTime = DateTimeOffset.Now.AddSeconds(delay.TotalSeconds).ToUnixTimeSeconds()
                });
            });
        }

        private NebulaHeader BuildNebulaHeader<T>(string group)
            where T : class, new()
        {
            var newId = _idWorker.NextId().ToString();
            var header = new NebulaHeader()
            {
                { NebulaHeader.MessageType, typeof(T).ToString() },
                { NebulaHeader.Sender, $"{Environment.MachineName}.{Assembly.GetEntryAssembly().GetName().Name}" },
                { NebulaHeader.SendTimeStamp, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() },
                { NebulaHeader.MessageId, newId },
                { NebulaHeader.RequestId, newId },
            };
            return header;
        }

        private NebulaHeader BuildNebulaHeader<T>(string group, IDictionary<string, string> headers)
            where T : class, new()
        {
            var newId = _idWorker.NextId().ToString();
            var header = new NebulaHeader(headers);
            header[NebulaHeader.MessageType] = typeof(T).ToString();
            header[NebulaHeader.Sender] = $"{Environment.MachineName}.{Assembly.GetEntryAssembly().GetName().Name}";
            header[NebulaHeader.SendTimeStamp] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            header[NebulaHeader.MessageId] = newId;
            if (string.IsNullOrEmpty(header[NebulaHeader.RequestId]))
                header[NebulaHeader.RequestId] = newId;
            return header;
        }
    }
}