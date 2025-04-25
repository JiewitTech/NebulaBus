using NebulaBus;
using NebulaBus.Store.Sql;
using SqlSugar;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NebulaSqlStoreExtension
    {
        public static void UseSqlStore(this NebulaOptions options, ConnectionConfig connectionConfig)
        {
            options.AddNebulaServiceProvider(new NebulaSqlStoreServiceProvider(connectionConfig));
        }
    }
}
