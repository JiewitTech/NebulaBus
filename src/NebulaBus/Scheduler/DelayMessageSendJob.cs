using NebulaBus.Store;
using Quartz;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NebulaBus.Scheduler
{
    internal class DelayMessageSendJob : IJob
    {
        private readonly IStore _store;
        private readonly IEnumerable<IProcessor> _processors;

        public DelayMessageSendJob()
        {
        }

        public DelayMessageSendJob(IStore store, IEnumerable<IProcessor> processors)
        {
            _store = store;
            _processors = processors;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var data = context.JobDetail.JobDataMap.GetString("data");
                var messageData = JsonConvert.DeserializeObject<DelayStoreMessage>(data!);
                if (messageData == null) return;

                Console.WriteLine("DelayMessageSendJob");
                foreach (var processor in _processors)
                {
                    await processor.Send(messageData.Group, messageData.Message, messageData.Header);
                }

                await _store.Delete(messageData.MessageId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // throw;
            }
        }
    }
}