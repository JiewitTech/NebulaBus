using NebulaBus;
using WebApplicationSample.Messages;

namespace WebApplicationSample.Handlers
{
    public class TestHandlerV1 : NebulaHandler<TestMessage>
    {
        public override string Name => "NebulaBus.TestHandler.V1";
        public override string Group => "NebulaBus.TestHandler";
        public override TimeSpan RetryDelay => TimeSpan.FromSeconds(0);
        public override int MaxRetryCount => 3;

        public override async Task Handle(TestMessage message, NebulaHeader header)
        {
            Console.WriteLine($"{DateTime.Now} Received Message {Name}:{message.Message}");
            throw new Exception("Test Exception");
        }
    }
}