using System;
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
            using var redisLock = _redisClient.Lock($"{RedisLockKey}.Add", 2);
            if (redisLock == null)
                throw new Exception("got redis lock failed");
            await _redisClient.HSetAsync(RedisKey, $"{delayStoreMessage.MessageId}", delayStoreMessage);
        }

        public async Task Delete(string messageId)
        {
            using var redisLock = _redisClient.Lock($"{RedisLockKey}.Delete", 2);
            if (redisLock == null)
                throw new Exception("got redis lock failed");
            await _redisClient.HDelAsync(RedisKey, $"{messageId}");
        }

        public async Task<Dictionary<string, DelayStoreMessage>> GetAll()
        {
            using var redisLock = _redisClient.Lock($"{RedisLockKey}.Delete", 10);
            if (redisLock == null)
                throw new Exception("got redis lock failed");
            var result = await _redisClient.HGetAllAsync<DelayStoreMessage>(RedisKey);
            return result;
        }
    }
}