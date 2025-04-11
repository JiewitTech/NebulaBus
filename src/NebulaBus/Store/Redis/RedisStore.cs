using CSRedis;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace NebulaBus.Store.Redis
{
    internal class RedisStore : IStore
    {
        private string RedisKey => $"NebulaBus:{_nebulaOptions.ClusterName}.Store";
        private string IndexRedisKey => $"NebulaBus:{_nebulaOptions.ClusterName}.StoreIndex";

        private readonly CSRedisClient _redisClient;
        private readonly NebulaOptions _nebulaOptions;
        private CSRedisClientLock _redisClientLock;

        public RedisStore(IServiceProvider provider, NebulaOptions nebulaOptions)
        {
            _redisClient = provider.GetKeyedService<CSRedisClient>("NebulaBusRedis")!;
            _nebulaOptions = nebulaOptions;
        }

        public async Task Add(DelayStoreMessage delayStoreMessage)
        {
            await _redisClient.ZAddAsync(IndexRedisKey, (delayStoreMessage.TriggerTime, $"{delayStoreMessage.MessageId}.{delayStoreMessage.Name}"));
            await _redisClient.HSetAsync(RedisKey, $"{delayStoreMessage.MessageId}.{delayStoreMessage.Name}",
                delayStoreMessage);
        }

        public async Task Delete(DelayStoreMessage delayStoreMessage)
        {
            await _redisClient.ZRemAsync(IndexRedisKey, $"{delayStoreMessage.MessageId}.{delayStoreMessage.Name}");
            await _redisClient.HDelAsync(RedisKey, $"{delayStoreMessage.MessageId}.{delayStoreMessage.Name}");
        }

        public async Task<DelayStoreMessage[]?> GetAllByKeys(long beforeTimestamp)
        {
            var keys = await _redisClient.ZRangeByScoreAsync(IndexRedisKey, 0, beforeTimestamp);
            if (keys == null) return null;
            var result = await _redisClient.HMGetAsync<DelayStoreMessage>(RedisKey, keys!);
            return result;
        }

        public bool Lock()
        {
            _redisClientLock = _redisClient.Lock($"NebulaBus:{_nebulaOptions.ClusterName}.Lock", 3, true);
            return _redisClientLock != null;
        }

        public void Dispose()
        {
            _redisClientLock?.Dispose();
            _redisClient?.Dispose();
        }
    }
}