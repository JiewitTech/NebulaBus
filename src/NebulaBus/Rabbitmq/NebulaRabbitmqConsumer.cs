using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaBus.Rabbitmq
{
    internal class NebulaRabbitmqConsumer : AsyncDefaultBasicConsumer
    {
        private readonly Func<string, NebulaHeader, Task> _excute;
        public NebulaRabbitmqConsumer(IChannel channel, int prefetchSize, Func<string, NebulaHeader, Task> excute) : base(channel)
        {
            _excute = excute;
        }

        public override async Task HandleBasicDeliverAsync(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IReadOnlyBasicProperties properties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
        {
            var message = Encoding.UTF8.GetString(body.Span);
            var header = new NebulaHeader();
            if (properties.Headers != null)
            {
                foreach (var item in properties.Headers!)
                {
                    if (item.Value is byte[] bytes) header.Add(item.Key, Encoding.UTF8.GetString(bytes));
                }
            }

            await _excute(message, header).ConfigureAwait(false);
            await BasicAckAsync(deliveryTag, cancellationToken);
        }

        public async Task BasicAckAsync(ulong deliveryTag, CancellationToken cancellationToken = default)
        {
            if (Channel.IsOpen)
                await Channel.BasicAckAsync(deliveryTag, false, cancellationToken);
        }
    }
}
