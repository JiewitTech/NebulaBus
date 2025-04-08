using System.Collections.Generic;
using System.Threading.Tasks;

namespace NebulaBus.Store
{
    internal interface IStore
    {
        Task Add(DelayStoreMessage delayStoreMessage);
        Task Delete(string messageId);
        Task<Dictionary<string, DelayStoreMessage>> GetAll();
        bool Lock();
    }
}