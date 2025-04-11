using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NebulaBus.Store.Redis
{
    internal class RedisStore : IStore
    {
        private string RedisKey => $"NebulaBus:{_nebulaOptions.ClusterName}.Store";
        private string IndexRedisKey => $"NebulaBus:{_nebulaOptions.ClusterName}.StoreIndex";

        private readonly RedisClient _redisClient;
        private readonly NebulaOptions _nebulaOptions;
        private RedisClient.LockController _redisClientLock;

        public RedisStore(IServiceProvider provider, NebulaOptions nebulaOptions)
        {
            _redisClient = provider.GetKeyedService<RedisClient>("NebulaBusRedis")!;
            _nebulaOptions = nebulaOptions;
        }

        public async Task Add(DelayStoreMessage delayStoreMessage)
        {
            //var redis = new CSRedis.CSRedisClient("");
            using (var tran = _redisClient.Multi())
            {
                tran.ZAdd(IndexRedisKey, delayStoreMessage.TriggerTime, $"{delayStoreMessage.MessageId}.{delayStoreMessage.Name}");
                tran.HSet(RedisKey, $"{delayStoreMessage.MessageId}.{delayStoreMessage.Name}", JsonConvert.SerializeObject(delayStoreMessage));
                tran.Exec();
            }
            await Task.CompletedTask;
        }

        public async Task Delete(DelayStoreMessage delayStoreMessage)
        {
            using var tran = _redisClient.Multi();
            await tran.ZRemAsync(IndexRedisKey, $"{delayStoreMessage.MessageId}.{delayStoreMessage.Name}");
            await tran.HDelAsync(RedisKey, $"{delayStoreMessage.MessageId}.{delayStoreMessage.Name}");
            tran.Exec();
        }

        public async Task<DelayStoreMessage[]?> Get(long beforeTimestamp)
        {
            var keys = _redisClient.ZRangeByScore(IndexRedisKey, 0, beforeTimestamp);
            if (keys == null || keys.Length == 0) return null;
            var result1 = await _redisClient.HMGetAsync<string>(RedisKey, keys!);
            return null;
            var result = await _redisClient.HMGetAsync<DelayStoreMessage>(RedisKey, keys!);
            //排除为空的值并删除
            for (var i = 0; i < keys.Length; i++)
            {
                if (result[i] == null)
                {
                    using var tran = _redisClient.Multi();
                    await RemoveKey(keys[i]);
                    await tran.ZRemAsync(IndexRedisKey, keys[i]);
                    await tran.HDelAsync(RedisKey, keys[i]);
                    tran.Exec();
                }
            }
            return result.Where(x => x != null).ToArray();
        }

        private async Task RemoveKey(string key)
        {
            using var tran = _redisClient.Multi();
            await tran.ZRemAsync(IndexRedisKey, key);
            await tran.HDelAsync(RedisKey, key);
            tran.Exec();
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