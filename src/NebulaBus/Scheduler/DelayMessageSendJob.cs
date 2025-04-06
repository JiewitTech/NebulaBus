using NebulaBus.Store;
using Quartz;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace NebulaBus.Scheduler
{
    internal class DelayMessageSendJob : IJob
    {
        private readonly IStore _store;
        private readonly IEnumerable<IProcessor> _processors;
        public DelayMessageSendJob(IStore store, IEnumerable<IProcessor> processors)
        {
            _store = store;
            _processors = processors;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var data = context.JobDetail.JobDataMap.GetString("data");
            var messageData = JsonSerializer.Deserialize<DelayStoreMessage>(data!);
            if (messageData == null) return;

            Console.WriteLine("DelayMessageSendJob");
            foreach (var processor in _processors)
            {
                await processor.Send(messageData.Group, messageData.Message, messageData.Header);
                await _store.Delete(messageData.MessageId);
            }
        }
    }
}