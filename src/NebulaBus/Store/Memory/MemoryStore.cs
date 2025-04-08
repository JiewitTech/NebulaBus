using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NebulaBus.Store.Memory
{
    internal class MemoryStore : IStore
    {
        private readonly ConcurrentDictionary<string, DelayStoreMessage> _storeMessages;

        public MemoryStore()
        {
            _storeMessages = new ConcurrentDictionary<string, DelayStoreMessage>();
        }

        public async Task Add(DelayStoreMessage delayStoreMessage)
        {
            _storeMessages.AddOrUpdate(delayStoreMessage.MessageId, c => delayStoreMessage,
                (c, o) => delayStoreMessage);
            await Task.CompletedTask;
        }

        public async Task Delete(string messageId)
        {
            _storeMessages.TryRemove(messageId, out _);
            await Task.CompletedTask;
        }

        public async Task<Dictionary<string, DelayStoreMessage>> GetAll()
        {
            return await Task.FromResult(_storeMessages.ToDictionary(x => x.Key, x => x.Value));
        }

        public bool Lock()
        {
            return true;
        }
    }
}