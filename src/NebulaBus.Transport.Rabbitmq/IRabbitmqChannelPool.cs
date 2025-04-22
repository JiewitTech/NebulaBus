using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace NebulaBus.Transport.Rabbitmq
{
    internal interface IRabbitmqChannelPool
    {
        Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default);
        Task ReturnChannel(IChannel channel);
    }
}