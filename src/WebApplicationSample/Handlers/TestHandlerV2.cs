using MessageLibrary;
using NebulaBus;

namespace WebApplicationSample.Handlers
{
    public class TestHandlerV2 : NebulaHandler<TestMessage>
    {
        public override string Name => "NebulaBus.TestHandler.V2";
        public override string Group => "NebulaBus.TestHandler";
        public override byte? ExecuteThreadCount => 4;

        protected override async Task Handle(TestMessage message, NebulaHeader header)
        {
            Console.WriteLine($"{DateTime.Now} [{Name}] [{nameof(TestHandlerV2)}]Received Message :{message.Message} RetryCount {header.GetRetryCount()}");
        }
    }
}