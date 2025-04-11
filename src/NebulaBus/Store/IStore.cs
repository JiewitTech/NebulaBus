using System;
using System.Threading.Tasks;

namespace NebulaBus.Store
{
    internal interface IStore : IDisposable
    {
        Task Add(DelayStoreMessage delayStoreMessage);
        Task Delete(DelayStoreMessage delayStoreMessage);
        Task<DelayStoreMessage[]?> GetAllByKeys(long beforeTimestamp);
        bool Lock();
    }
}