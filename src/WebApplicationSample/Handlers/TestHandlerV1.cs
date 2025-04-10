using NebulaBus;
using WebApplicationSample.Messages;

namespace WebApplicationSample.Handlers
{
    public class TestHandlerV1 : NebulaHandler<TestMessage>
    {
        public override string Name => "NebulaBus.TestHandler.V1";
        public override string Group => "NebulaBus.TestHandler";
        public override TimeSpan RetryDelay => TimeSpan.FromSeconds(10);
        public override int MaxRetryCount => 5;

        private readonly INebulaBus _bus;

        public TestHandlerV1(INebulaBus nebulaBus)
        {
            _bus = nebulaBus;
        }

        protected override async Task Handle(TestMessage message, NebulaHeader header)
        {
            Console.WriteLine($"{DateTime.Now} Received Message {Name}:{message.Message} Header:{header["customHeader"]} RetryCount:{header[NebulaHeader.RetryCount]}");
            throw new Exception("Test Exception");
        }
    }
}