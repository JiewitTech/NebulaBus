using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NebulaBus
{
    internal class NebulaBusService : INebulaBus
    {
        public NebulaBusService()
        {

        }

        public Task PublishAsync<T>(string name, T message)
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync<T>(string name, T message, IDictionary<string, string> headers)
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync<T>(TimeSpan delay, string name, T message)
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync<T>(TimeSpan delay, string name, T message, IDictionary<string, string> headers)
        {
            throw new NotImplementedException();
        }

        public Task<Response> PublishAsync<Request, Response>(string name, Request message)
        {
            throw new NotImplementedException();
        }

        public Task<Response> PublishAsync<Request, Response>(string name, Request message, IDictionary<string, string> headers)
        {
            throw new NotImplementedException();
        }

        public Task<Response> PublishAsync<Request, Response>(TimeSpan delay, string name, Request message)
        {
            throw new NotImplementedException();
        }

        public Task<Response> PublishAsync<Request, Response>(TimeSpan delay, string name, Request message, IDictionary<string, string> headers)
        {
            throw new NotImplementedException();
        }
    }
}
