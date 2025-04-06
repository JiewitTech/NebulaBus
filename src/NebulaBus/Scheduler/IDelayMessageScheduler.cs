using NebulaBus.Store;
using System.Threading.Tasks;

namespace NebulaBus.Scheduler
{
    internal interface IDelayMessageScheduler
    {
        Task StartStoreSchedule();
        Task StartSenderScheduler();
        Task StartSchedule(DelayStoreMessage delayStoreMessage);
    }
}