using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaBus.Rabbitmq
{
    internal class NebulaRabbitmqConsumer : AsyncDefaultBasicConsumer
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly Type _handlerType;
        public NebulaRabbitmqConsumer(IChannel channel, IServiceScopeFactory serviceScopeFactory, Type handlerType) : base(channel)
        {
            _handlerType = handlerType;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public override async Task HandleBasicDeliverAsync(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IReadOnlyBasicProperties properties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
        {
            var header = new NebulaHeader();
            if (properties.Headers != null)
            {
                foreach (var item in properties.Headers!)
                {
                    if (item.Value is byte[] bytes) header.Add(item.Key, Encoding.UTF8.GetString(bytes));
                }
            }
            using var scope = _serviceScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetService(_handlerType) as NebulaHandler;
            if (handler != null)
            {
                await handler.Excute(scope.ServiceProvider, body, header);
            }
            await BasicAckAsync(deliveryTag, cancellationToken);
        }

        public async Task BasicAckAsync(ulong deliveryTag, CancellationToken cancellationToken = default)
        {
            if (Channel.IsOpen)
                await Channel.BasicAckAsync(deliveryTag, false, cancellationToken);
        }
    }
}
