using NebulaBus.Scheduler;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using NebulaBus.Store;

namespace NebulaBus
{
    internal class NebulaBusService : INebulaBus
    {
        private readonly IEnumerable<IProcessor> _processors;
        private readonly IDelayMessageScheduler _delayMessageScheduler;

        public NebulaBusService(IEnumerable<IProcessor> processors, IDelayMessageScheduler delayMessageScheduler)
        {
            _processors = processors;
            _delayMessageScheduler = delayMessageScheduler;
        }

        public async Task PublishAsync<T>(string group, T message)
        {
            var header = BuildNebulaHeader<T>(group);
            foreach (var processor in _processors)
            {
                await processor.Send(group, JsonSerializer.Serialize(message), header);
            }
        }

        public async Task PublishAsync<T>(string group, T message, IDictionary<string, string?> headers)
        {
            var header = BuildNebulaHeader<T>(group, headers);
            foreach (var processor in _processors)
            {
                await processor.Send(group, JsonSerializer.Serialize(message), header);
            }
        }

        public async Task PublishAsync<T>(TimeSpan delay, string group, T message)
        {
            var header = BuildNebulaHeader<T>(group);
            await _delayMessageScheduler.Schedule(new DelayStoreMessage()
            {
                MessageId = header[NebulaHeader.MessageId]!,
                Group = group,
                Header = header,
                Message = JsonSerializer.Serialize(message),
                Name = "",
                TriggerTime = DateTimeOffset.Now.AddSeconds(delay.TotalSeconds)
            });
        }

        public async Task PublishAsync<T>(TimeSpan delay, string group, T message, IDictionary<string, string?> headers)
        {
            var header = BuildNebulaHeader<T>(group, headers);
            await _delayMessageScheduler.Schedule(new DelayStoreMessage()
            {
                MessageId = header[NebulaHeader.MessageId]!,
                Group = group,
                Header = header,
                Message = JsonSerializer.Serialize(message),
                Name = "",
                TriggerTime = DateTimeOffset.Now.AddSeconds(delay.TotalSeconds)
            });
        }

        private static NebulaHeader BuildNebulaHeader<T>(string group)
        {
            var newId = Guid.NewGuid().ToString();
            var header = new NebulaHeader()
            {
                { NebulaHeader.MessageType, typeof(T).ToString() },
                { NebulaHeader.Group, group },
                { NebulaHeader.Sender, Environment.MachineName },
                { NebulaHeader.SendTimeStamp, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() },
                { NebulaHeader.MessageId, newId },
                { NebulaHeader.RequestId, newId },
            };
            return header;
        }

        private static NebulaHeader BuildNebulaHeader<T>(string group, IDictionary<string, string?> headers)
        {
            var newId = Guid.NewGuid().ToString();
            var header = new NebulaHeader(headers);
            header[NebulaHeader.MessageType] = typeof(T).ToString();
            header[NebulaHeader.Group] = group;
            header[NebulaHeader.Sender] = Environment.MachineName;
            header[NebulaHeader.SendTimeStamp] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            header[NebulaHeader.MessageId] = newId;
            if (string.IsNullOrEmpty(header[NebulaHeader.RequestId]))
                header[NebulaHeader.RequestId] = newId;
            return header;
        }
    }
}