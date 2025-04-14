using RabbitMQ.Client;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaBus.Rabbitmq
{
    internal interface IRabbitmqChannelPool
    {
        Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default);
        Task ReturnChannel(IChannel channel);
    }
}
