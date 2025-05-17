using Microsoft.Extensions.Logging;
using NebulaBus.Store;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NebulaBus.Scheduler
{
    internal class DelayMessageSendJob : IJob
    {
        private readonly IStore _store;
        private readonly IEnumerable<ITransport> _processors;
        private readonly ILogger<DelayMessageSendJob> _logger;
        private readonly JsonSerializerOptions _serializerOptions;

        public DelayMessageSendJob(IStore store, IEnumerable<ITransport> processors,
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
            context.JobDetail.JobDataMap.TryGetString("messageId", out var messageId);
            context.JobDetail.JobDataMap.TryGetString("requestId", out var requestId);
            context.JobDetail.JobDataMap.TryGetString("name", out var name);
            try
            {
                var hasData = context.JobDetail.JobDataMap.TryGetString("data", out var data);
                if (!hasData) return;

                var messageData = JsonSerializer.Deserialize<NebulaStoreMessage>(data!, _serializerOptions);
                if (messageData == null) return;

                if (string.IsNullOrEmpty(messageData.Transport))
                {
                    foreach (var processor in _processors)
                    {
                        messageData.Header[NebulaHeader.Transport] = processor.Name;
                        await processor.Publish(messageData.Name, messageData.Message, messageData.Header);
                    }
                }
                else
                {
                    var transport = _processors.SingleOrDefault(x => x.Name == messageData.Transport);
                    if (transport != null)
                    {
                        await transport.Publish(messageData.Name, messageData.Message, messageData.Header);
                    }
                }
                _store.Delete(messageData);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"[{name}]-[{messageId}]-[{requestId}] Error while sending delay message");
            }
        }
    }
}