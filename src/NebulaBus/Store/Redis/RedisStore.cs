using CSRedis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NebulaBus.Store.Redis
{
    internal class RedisStore : IStore
    {
        private const string RedisKey = "NebulaBus.DelayQueue";

        private readonly CSRedisClient _redisClient;
        public RedisStore(CSRedisClient cSRedisClient)
        {
            _redisClient = cSRedisClient;
        }

        public async Task Add(DelayStoreMessage delayStoreMessage)
        {
            await _redisClient.HSetAsync(RedisKey, $"{delayStoreMessage.MessageId}", delayStoreMessage);
        }

        public async Task Delete(string messageId)
        {
            await _redisClient.HDelAsync(RedisKey, $"{messageId}");
        }

        public async Task<Dictionary<string, DelayStoreMessage>> GetAll()
        {
            var result = await _redisClient.HGetAllAsync<DelayStoreMessage>(RedisKey);
            return result;
        }
    }
}