using System.Threading;
using NebulaBus.Store;
using System.Threading.Tasks;

namespace NebulaBus.Scheduler
{
    internal interface IDelayMessageScheduler
    {
        Task StartStoreSchedule(CancellationToken cancellationToken);
        Task StartSenderScheduler();
        Task Schedule(DelayStoreMessage delayStoreMessage);
    }
}