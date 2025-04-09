using System;
using CSRedis;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace NebulaBus.Store.Redis
{
    internal class RedisStore : IStore
    {
        private const string RedisKey = "NebulaBus:DelayMessage";

        private readonly CSRedisClient _redisClient;
        private readonly NebulaOptions _nebulaOptions;

        public RedisStore(IServiceProvider provider, NebulaOptions nebulaOptions)
        {
            _redisClient = provider.GetKeyedService<CSRedisClient>("NebulaBusRedis")!;
            _nebulaOptions = nebulaOptions;
        }

        public async Task Add(DelayStoreMessage delayStoreMessage)
        {
            await _redisClient.HSetAsync(RedisKey, $"{delayStoreMessage.MessageId}.{delayStoreMessage.Name}",
                delayStoreMessage);
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
            var redisLock = _redisClient.Lock($"NebulaBus:{_nebulaOptions.ClusterName}", 1, true);
            return redisLock != null;
        }
    }
}