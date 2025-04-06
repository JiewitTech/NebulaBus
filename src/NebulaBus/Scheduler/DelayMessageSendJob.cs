using NebulaBus.Store;
using Quartz;
using System;
using System.Threading.Tasks;

namespace NebulaBus.Scheduler
{
    internal class DelayMessageSendJob : IJob
    {
        private readonly IStore _store;
        public DelayMessageSendJob(IStore store)
        {
            _store = store;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("DelayMessageSendJob");
        }
    }
}
