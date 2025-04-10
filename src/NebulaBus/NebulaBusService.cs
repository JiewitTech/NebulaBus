using Microsoft.Extensions.DependencyInjection;
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

        public NebulaBusService(IServiceProvider serviceProvider)
        {
            _processors = serviceProvider.GetRequiredService<IEnumerable<IProcessor>>();
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
                await processor.Publish(nameOrGroup, JsonConvert.SerializeObject(message), header);
            }
        }

        public async Task PublishAsync<T>(string nameOrGroup, T message, IDictionary<string, string> headers)
            where T : class, new()
        {
            var header = BuildNebulaHeader<T>(nameOrGroup, headers);
            foreach (var processor in _processors)
            {
                await processor.Publish(nameOrGroup, JsonConvert.SerializeObject(message), header);
            }
        }

        public async Task PublishAsync<T>(TimeSpan delay, string nameOrGroup, T message)
            where T : class, new()
        {
            var header = BuildNebulaHeader<T>(nameOrGroup);
            await _delayMessageScheduler.Schedule(new DelayStoreMessage()
            {
                MessageId = header[NebulaHeader.MessageId]!,
                Group = nameOrGroup,
                Header = header,
                Message = JsonConvert.SerializeObject(message),
                Name = nameOrGroup,
                TriggerTime = DateTimeOffset.Now.AddSeconds(delay.TotalSeconds).ToUnixTimeSeconds()
            });
        }

        public async Task PublishAsync<T>(TimeSpan delay, string nameOrGroup, T message, IDictionary<string, string> headers)
            where T : class, new()
        {
            var header = BuildNebulaHeader<T>(nameOrGroup, headers);
            await _delayMessageScheduler.Schedule(new DelayStoreMessage()
            {
                MessageId = header[NebulaHeader.MessageId]!,
                Group = nameOrGroup,
                Header = header,
                Message = JsonConvert.SerializeObject(message),
                Name = nameOrGroup,
                TriggerTime = DateTimeOffset.Now.AddSeconds(delay.TotalSeconds).ToUnixTimeSeconds()
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