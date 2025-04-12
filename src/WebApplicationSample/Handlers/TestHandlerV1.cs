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
        public override TimeSpan RetryInterval => TimeSpan.FromSeconds(10);
        public override byte? ExecuteThreadCount => 2;

        private readonly INebulaBus _bus;

        public TestHandlerV1(INebulaBus nebulaBus)
        {
            _bus = nebulaBus;
        }

        protected override async Task Handle(TestMessage message, NebulaHeader header)
        {
            Console.WriteLine($"{DateTime.Now} [{Name}] [{nameof(TestHandlerV1)}]Received Message :{message.Message} RetryCount {header.GetRetryCount()}");
            throw new Exception("Test Exception");
        }
    }
}