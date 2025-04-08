using CSRedis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NebulaBus.Store.Redis
{
    internal class RedisStore : IStore
    {
        private const string RedisKey = "NebulaBus:DelayMessage";
        private const string RedisLockKey = "NebulaBus:Lock";

        private readonly CSRedisClient _redisClient;

        public RedisStore(CSRedisClient cSRedisClient)
        {
            _redisClient = cSRedisClient;
        }

        public async Task Add(DelayStoreMessage delayStoreMessage)
        {
            await _redisClient.HSetAsync(RedisKey, $"{delayStoreMessage.MessageId}.{delayStoreMessage.Name}", delayStoreMessage);
        }

        public async Task Delete(DelayStoreMessage delayStoreMessage)
        {
            await _redisClient.HDelAsync(RedisKey, $"{delayStoreMessage.MessageId}.{delayStoreMessage.Name}");
        }

        public async Task<Dictionary<string, DelayStoreMessage>> GetAll()
        {
            var result = await _redisClient.HGetAllAsync<DelayStoreMessage>(RedisKey);
            return result;
        }

        public bool Lock()
        {
            var redisLock = _redisClient.Lock($"{RedisLockKey}.Lock", 1, true);
            return redisLock != null;
        }
    }
}