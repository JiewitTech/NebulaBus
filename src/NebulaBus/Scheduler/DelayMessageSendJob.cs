using NebulaBus.Store;
using Quartz;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NebulaBus.Scheduler
{
    internal class DelayMessageSendJob : IJob
    {
        private readonly IStore _store;
        private readonly IEnumerable<IProcessor> _processors;
        private readonly ILogger<DelayMessageSendJob> _logger;

        public DelayMessageSendJob(IStore store, IEnumerable<IProcessor> processors,
            ILogger<DelayMessageSendJob> logger)
        {
            _store = store;
            _processors = processors;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var messageId = context.JobDetail.JobDataMap.GetString("messageId");
            var requestId = context.JobDetail.JobDataMap.GetString("requestId");
            var name = context.JobDetail.JobDataMap.GetString("name");
            try
            {
                var data = context.JobDetail.JobDataMap.GetString("data");
                var messageData = JsonConvert.DeserializeObject<DelayStoreMessage>(data!);
                if (messageData == null) return;
                foreach (var processor in _processors)
                {
                    await processor.Send(messageData.Group, messageData.Message, messageData.Header);
                }

                await _store.Delete(messageData.MessageId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"[{name}]-[{messageId}]-[{requestId}] Error while sending delay message");
            }
        }
    }
}