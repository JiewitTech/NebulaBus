using NebulaBus.Scheduler;
using NebulaBus.Store;
using Newtonsoft.Json;
using Snowflake.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NebulaBus
{
    internal class NebulaBusService : INebulaBus
    {
        private readonly IEnumerable<IProcessor> _processors;
        private readonly IDelayMessageScheduler _delayMessageScheduler;
        private readonly IdWorker _idWorker;

        public NebulaBusService(IEnumerable<IProcessor> processors, IDelayMessageScheduler delayMessageScheduler)
        {
            _processors = processors;
            _delayMessageScheduler = delayMessageScheduler;
            if (!long.TryParse(Environment.GetEnvironmentVariable("NEBULABUS_WORKERID"), out var result))
                result = 1;
            _idWorker = new IdWorker(result, 1);
        }

        public async Task PublishAsync<T>(string group, T message)
            where T : class, new()
        {
            var header = BuildNebulaHeader<T>(group);
            foreach (var processor in _processors)
            {
                await processor.Publish(group, JsonConvert.SerializeObject(message), header);
            }
        }

        public async Task PublishAsync<T>(string group, T message, IDictionary<string, string> headers)
            where T : class, new()
        {
            var header = BuildNebulaHeader<T>(group, headers);
            foreach (var processor in _processors)
            {
                await processor.Publish(group, JsonConvert.SerializeObject(message), header);
            }
        }

        public async Task PublishAsync<T>(TimeSpan delay, string group, T message)
            where T : class, new()
        {
            var header = BuildNebulaHeader<T>(group);
            await _delayMessageScheduler.Schedule(new DelayStoreMessage()
            {
                MessageId = header[NebulaHeader.MessageId]!,
                Group = group,
                Header = header,
                Message = JsonConvert.SerializeObject(message),
                Name = group,
                TriggerTime = DateTimeOffset.Now.AddSeconds(delay.TotalSeconds)
            });
        }

        public async Task PublishAsync<T>(TimeSpan delay, string group, T message, IDictionary<string, string> headers)
            where T : class, new()
        {
            var header = BuildNebulaHeader<T>(group, headers);
            await _delayMessageScheduler.Schedule(new DelayStoreMessage()
            {
                MessageId = header[NebulaHeader.MessageId]!,
                Group = group,
                Header = header,
                Message = JsonConvert.SerializeObject(message),
                Name = group,
                TriggerTime = DateTimeOffset.Now.AddSeconds(delay.TotalSeconds)
            });
        }

        private NebulaHeader BuildNebulaHeader<T>(string group)
            where T : class, new()
        {
            var newId = _idWorker.NextId().ToString();
            var header = new NebulaHeader()
            {
                { NebulaHeader.MessageType, typeof(T).ToString() },
                { NebulaHeader.Sender, Environment.MachineName },
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
            header[NebulaHeader.Sender] = Environment.MachineName;
            header[NebulaHeader.SendTimeStamp] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            header[NebulaHeader.MessageId] = newId;
            if (string.IsNullOrEmpty(header[NebulaHeader.RequestId]))
                header[NebulaHeader.RequestId] = newId;
            return header;
        }
    }
}