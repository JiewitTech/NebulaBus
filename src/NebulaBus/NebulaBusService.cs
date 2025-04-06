using NebulaBus.Scheduler;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

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
            foreach (var processor in _processors)
            {
                await processor.Send(group, JsonSerializer.Serialize(message), new NebulaHeader());
            }
        }

        public Task PublishAsync<T>(string group, T message, IDictionary<string, string> headers)
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync<T>(TimeSpan delay, string group, T message)
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync<T>(TimeSpan delay, string group, T message, IDictionary<string, string> headers)
        {
            throw new NotImplementedException();
        }

        public Task<Response> PublishAsync<Request, Response>(string group, Request message)
        {
            throw new NotImplementedException();
        }

        public Task<Response> PublishAsync<Request, Response>(string group, Request message, IDictionary<string, string> headers)
        {
            throw new NotImplementedException();
        }

        public Task<Response> PublishAsync<Request, Response>(TimeSpan delay, string group, Request message)
        {
            throw new NotImplementedException();
        }

        public Task<Response> PublishAsync<Request, Response>(TimeSpan delay, string group, Request message, IDictionary<string, string> headers)
        {
            throw new NotImplementedException();
        }
    }
}
