using System;
using System.Threading.Tasks;

namespace NebulaBus.Store
{
    internal interface IStore : IDisposable
    {
        void Add(NebulaStoreMessage nebulaStoreMessage);
        void Delete(NebulaStoreMessage nebulaStoreMessage);
        Task<NebulaStoreMessage[]?> Get(long beforeTimestamp);
        void RefreshLock();
        bool Lock(string value);
        void UnLock(string value);
    }
}