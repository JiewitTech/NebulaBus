using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NebulaBus
{
    public interface INebulaBus
    {
        Task PublishAsync<T>(string name, T message);
        Task PublishAsync<T>(string name, T message, IDictionary<string, string> headers);
        Task PublishAsync<T>(TimeSpan delay, string name, T message);
        Task PublishAsync<T>(TimeSpan delay, string name, T message, IDictionary<string, string> headers);

        Task<Response> PublishAsync<Request, Response>(string name, Request message);
        Task<Response> PublishAsync<Request, Response>(string name, Request message, IDictionary<string, string> headers);
        Task<Response> PublishAsync<Request, Response>(TimeSpan delay, string name, Request message);
        Task<Response> PublishAsync<Request, Response>(TimeSpan delay, string name, Request message, IDictionary<string, string> headers);
    }
}