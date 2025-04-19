using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NebulaBus.Store.Redis
{
    internal class RedisStore : IStore
    {
        private string RedisKey => $"NebulaBus:{_nebulaOptions.ClusterName}.Store";
        private string IndexRedisKey => $"NebulaBus:{_nebulaOptions.ClusterName}.StoreIndex";
        private string LockKey => $"NebulaBus:{_nebulaOptions.ClusterName}.Lock";

        private readonly RedisClient _redisClient;
        private readonly NebulaOptions _nebulaOptions;
        private RedisClient.LockController _redisClientLock;
        private readonly ILogger<RedisStore> _logger;

        public RedisStore(IServiceProvider provider, NebulaOptions nebulaOptions, ILogger<RedisStore> logger)
        {
            _redisClient = provider.GetKeyedService<RedisClient>("NebulaBusRedis")!;
            _nebulaOptions = nebulaOptions;
            _logger = logger;
        }

        public void Add(NebulaStoreMessage nebulaStoreMessage)
        {
            using var tran = _redisClient.Multi();
            tran.ZAdd(IndexRedisKey, nebulaStoreMessage.TriggerTime,nebulaStoreMessage.GetKey());
            tran.HSet(RedisKey, nebulaStoreMessage.GetKey(),
                nebulaStoreMessage);
            tran.Exec();
        }

        public void Delete(NebulaStoreMessage nebulaStoreMessage)
        {
            using var tran = _redisClient.Multi();
            tran.ZRem(IndexRedisKey, nebulaStoreMessage.GetKey());
            tran.HDel(RedisKey, nebulaStoreMessage.GetKey());
            tran.Exec();
        }

        public async Task<NebulaStoreMessage[]?> Get(long beforeTimestamp)
        {
            var keys = _redisClient.ZRangeByScore(IndexRedisKey, 0, beforeTimestamp);
            if (keys == null || keys.Length == 0) return null;
            var result = await _redisClient.HMGetAsync<NebulaStoreMessage>(RedisKey, keys!);
            //排除为空的值并删除
            for (var i = 0; i < keys.Length; i++)
            {
                if (result[i] == null)
                {
                    RemoveKey(keys[i]);
                }
            }

            return result.Where(x => x != null).ToArray();
        }

        private void RemoveKey(string key)
        {
            using var tran = _redisClient.Multi();
            tran.ZRem(IndexRedisKey, key);
            tran.HDel(RedisKey, key);
            tran.Exec();
        }

        public bool Lock(string value)
        {
            var val = _redisClient.Get<string>(LockKey);
            if (val == value) return true;
            return _redisClient.SetNx(LockKey, value, 3);
        }

        public void RefreshLock()
        {
            _redisClient.Expire(LockKey, 3);
        }

        public void UnLock(string value)
        {
            var val = _redisClient.Get<string>(LockKey);
            if (val == value)
                _redisClient.Del(LockKey);
        }

        public void Dispose()
        {
            try
            {
                _redisClient?.Dispose();
                _redisClientLock?.Dispose();
            }
            catch
            {
            }
        }
    }
}