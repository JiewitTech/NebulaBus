using System.Threading;
using NebulaBus.Store;
using System.Threading.Tasks;

namespace NebulaBus.Scheduler
{
    internal interface IDelayMessageScheduler
    {
        Task StartSchedule(CancellationToken cancellationToken);
        void Schedule(DelayStoreMessage delayStoreMessage);
    }
}