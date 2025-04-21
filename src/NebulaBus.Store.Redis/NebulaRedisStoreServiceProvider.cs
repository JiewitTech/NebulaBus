using System.Linq;
using System.Text.Json;
using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NebulaBus.Store.Redis
{
    internal class NebulaRedisStoreServiceProvider : INebulaServiceProvider
    {
        private readonly string _redisConnectionString;

        public NebulaRedisStoreServiceProvider(string redisConnectionString)
        {
            _redisConnectionString = redisConnectionString;
        }

        public void ProvideServices(IServiceCollection services, NebulaOptions options)
        {
            RedisClient freeRedisClient;
            if (_redisConnectionString.Contains(";"))
            {
                var connectionStringBuilders = _redisConnectionString.Split(";")
                    .Select(x => ConnectionStringBuilder.Parse(x)).ToArray();
                freeRedisClient = new RedisClient(connectionStringBuilders);
            }
            else
            {
                freeRedisClient = new RedisClient(_redisConnectionString);
            }

            freeRedisClient.Serialize = obj => JsonSerializer.Serialize(obj, options.JsonSerializerOptions);
            freeRedisClient.Deserialize = (json, type) =>
                JsonSerializer.Deserialize(json, type, options.JsonSerializerOptions);
            services.AddKeyedSingleton("NebulaBusRedis", freeRedisClient);
            services.TryAddSingleton<IStore, RedisStore>();
        }
    }
}