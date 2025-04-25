using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NebulaBus.Store.Sql
{
    internal class SqlStore : IStore
    {
        private string LockKey => $"NebulaBus:{_nebulaOptions.ClusterName}.Lock";
        private readonly SqlSugarScope _sqlClient;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly NebulaOptions _nebulaOptions;
        public SqlStore(IServiceProvider serviceProvider, NebulaOptions nebulaOptions)
        {
            _sqlClient = serviceProvider.GetRequiredKeyedService<SqlSugarScope>("NebulaBusSqlClient");
            _jsonSerializerOptions = nebulaOptions.JsonSerializerOptions;
            _nebulaOptions = nebulaOptions;
        }

        public void Add(NebulaStoreMessage nebulaStoreMessage)
        {
            _sqlClient.Insertable(new StoreEntity()
            {
                Id = nebulaStoreMessage.GetKey(),
                Content = JsonSerializer.Serialize(nebulaStoreMessage, _jsonSerializerOptions),
                TimeStamp = nebulaStoreMessage.TriggerTime
            }).ExecuteCommand();
        }

        public void Delete(NebulaStoreMessage nebulaStoreMessage)
        {
            _sqlClient.Deleteable<StoreEntity>().Where(x => x.Id == nebulaStoreMessage.GetKey()).ExecuteCommand();
        }

        public void Dispose()
        {
            _sqlClient?.Dispose();
        }

        public async Task<NebulaStoreMessage[]?> Get(long beforeTimestamp)
        {
            var entities = await _sqlClient.Queryable<StoreEntity>().Where(x => x.TimeStamp <= beforeTimestamp).ToListAsync();
            if (entities == null) return null;
            var result = entities.Select(x => JsonSerializer.Deserialize<NebulaStoreMessage>(x.Content, _jsonSerializerOptions)).ToArray();
            return result!;
        }

        public bool Lock(string value)
        {
            try
            {
                var now = DateTimeOffset.Now.ToUnixTimeSeconds();
                var x = _sqlClient.Storageable(new LockEntity()
                {
                    Id = LockKey,
                    Value = value,
                    ExpireTime = now + 3
                }).ToStorage();

                //不存在插入表示拿到锁
                var result = x.AsInsertable.ExecuteCommand();
                if (result == 1) return true;

                //存在但锁过期，更新锁并拿到锁
                result = x.AsUpdateable.Where(x => x.Id == LockKey && x.ExpireTime < now).ExecuteCommand();
                if (result == 1) return true;
                return false;
            }
            catch
            {
                return false;
            }
        }

        public void RefreshLock()
        {
            var expire = DateTimeOffset.Now.ToUnixTimeSeconds() + 3;
            _sqlClient.Updateable<LockEntity>().Where(x => x.Id == LockKey).SetColumns(x => new LockEntity()
            {
                ExpireTime = expire
            }).ExecuteCommand();
        }

        public void UnLock(string value)
        {
            _sqlClient.Deleteable<LockEntity>().Where(x => x.Id == LockKey && x.Value == value).ExecuteCommand();
        }

        public void Init()
        {
            _sqlClient.CodeFirst.InitTables(typeof(LockEntity));
            _sqlClient.CodeFirst.InitTables(typeof(StoreEntity));
        }
    }
}
