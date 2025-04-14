using RabbitMQ.Client;
using System.Threading.Tasks;

namespace NebulaBus.Rabbitmq
{
    internal interface IRabbitmqChannelPool
    {
        Task<IChannel> GetChannelAsync();
        Task ReturnChannel(IChannel channel);
    }
}
