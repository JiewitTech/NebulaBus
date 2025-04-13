using Microsoft.Extensions.Logging;
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
        private readonly ILogger<DelayMessageSendJob> _logger;
        private readonly JsonSerializerOptions _serializerOptions;

        public DelayMessageSendJob(IStore store, IEnumerable<IProcessor> processors,
            NebulaOptions nebulaOptions,
            ILogger<DelayMessageSendJob> logger)
        {
            _store = store;
            _processors = processors;
            _logger = logger;
            _serializerOptions = nebulaOptions.JsonSerializerOptions;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var messageId = context.JobDetail.JobDataMap.GetString("messageId");
            var requestId = context.JobDetail.JobDataMap.GetString("requestId");
            var name = context.JobDetail.JobDataMap.GetString("name");
            try
            {
                var data = context.JobDetail.JobDataMap.GetString("data");
                if (string.IsNullOrEmpty(data)) return;
                var messageData = JsonSerializer.Deserialize<DelayStoreMessage>(data, _serializerOptions);
                if (messageData == null) return;
                foreach (var processor in _processors)
                {
                    await processor.Publish(messageData.Name, messageData.Message, messageData.Header);
                }

                await _store.Delete(messageData);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"[{name}]-[{messageId}]-[{requestId}] Error while sending delay message");
            }
        }
    }
}