using NebulaBus;
using NebulaBus.Store.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NebulaRedisStoreExtension
    {
        public static void UseRedisStore(this NebulaOptions options, string redisConnectionString)
        {
            options.AddNebulaServiceProvider(new NebulaRedisStoreServiceProvider(redisConnectionString));
        }
    }
}