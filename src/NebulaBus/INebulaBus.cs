using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NebulaBus
{
    public interface INebulaBus
    {
        Task PublishAsync<T>(string name, T message) where T : class, new();
        Task PublishAsync<T>(string name, T message, IDictionary<string, string> headers) where T : class, new();
        Task PublishAsync<T>(TimeSpan delay, string name, T message) where T : class, new();
        Task PublishAsync<T>(TimeSpan delay, string name, T message, IDictionary<string, string> headers)
            where T : class, new();
    }
}