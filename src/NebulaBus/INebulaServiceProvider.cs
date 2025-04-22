using Microsoft.Extensions.DependencyInjection;

namespace NebulaBus
{
    public interface INebulaServiceProvider
    {
        void ProvideServices(IServiceCollection services, NebulaOptions options);
    }
}