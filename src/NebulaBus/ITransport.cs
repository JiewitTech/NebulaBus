using System;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaBus
{
    public interface ITransport : IDisposable
    {
        string Name { get; }
        Task Start(CancellationToken cancellationToken);
        Task Publish(string routingKey, object message, NebulaHeader header);
    }
}