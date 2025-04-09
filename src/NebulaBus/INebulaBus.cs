using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NebulaBus
{
    public interface INebulaBus
    {
        Task PublishAsync<T>(string nameOrGroup, T message) where T : class, new();
        Task PublishAsync<T>(string nameOrGroup, T message, IDictionary<string, string> headers) where T : class, new();
        Task PublishAsync<T>(TimeSpan delay, string nameOrGroup, T message) where T : class, new();
        Task PublishAsync<T>(TimeSpan delay, string nameOrGroup, T message, IDictionary<string, string> headers)
            where T : class, new();
    }
}