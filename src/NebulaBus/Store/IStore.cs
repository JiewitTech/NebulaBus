using System;
using System.Threading.Tasks;

namespace NebulaBus.Store
{
    internal interface IStore : IDisposable
    {
        void Add(DelayStoreMessage delayStoreMessage);
        void Delete(DelayStoreMessage delayStoreMessage);
        Task<DelayStoreMessage[]?> Get(long beforeTimestamp);
        void RefreshLock();
        bool Lock(string value);
        void UnLock(string value);
    }
}