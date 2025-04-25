using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SqlSugar;

namespace NebulaBus.Store.Sql
{
    internal class NebulaSqlStoreServiceProvider : INebulaServiceProvider
    {
        private readonly ConnectionConfig _connectionConfig;

        public NebulaSqlStoreServiceProvider(ConnectionConfig connectionConfig)
        {
            _connectionConfig = connectionConfig;
        }

        public void ProvideServices(IServiceCollection services, NebulaOptions options)
        {
            _connectionConfig.IsAutoCloseConnection = true;
            services.AddKeyedSingleton("NebulaBusSqlClient", new SqlSugarScope(_connectionConfig));
            services.TryAddSingleton<IStore, SqlStore>();
        }
    }
}
