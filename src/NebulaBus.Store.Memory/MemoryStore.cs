using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace NebulaBus.Store.Memory
{
    internal class MemoryStore : IStore
    {
        private readonly ConcurrentDictionary<string, NebulaStoreMessage> _storeMessages;

        public MemoryStore()
        {
            _storeMessages = new ConcurrentDictionary<string, NebulaStoreMessage>();
        }

        public void Add(NebulaStoreMessage nebulaStoreMessage)
        {
            _storeMessages.AddOrUpdate($"{nebulaStoreMessage.MessageId}.{nebulaStoreMessage.Name}", c => nebulaStoreMessage,
                (c, o) => nebulaStoreMessage);
        }

        public void Delete(NebulaStoreMessage nebulaStoreMessage)
        {
            _storeMessages.TryRemove($"{nebulaStoreMessage.MessageId}.{nebulaStoreMessage.Name}", out _);
        }

        public async Task<NebulaStoreMessage[]?> Get(long beforeTimestamp)
        {
            var result = _storeMessages.Values.ToArray();
            return await Task.FromResult(result);
        }

        public void Dispose()
        {
        }

        public void RefreshLock()
        {
        }

        public bool Lock(string value)
        {
            return true;
        }

        public void UnLock(string value)
        {
        }
    }
}