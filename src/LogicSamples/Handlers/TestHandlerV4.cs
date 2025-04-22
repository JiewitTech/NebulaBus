using MessageLibrary;
using NebulaBus;

namespace LogicSamples.Handlers
{
    public class TestHandlerV4 : NebulaHandler<TestMessage>
    {
        public override string Name => "NebulaBus.TestHandler.V4";
        public override string Group => "NebulaBus.TestHandler";
        public override byte? ExecuteThreadCount => 4;

        protected override async Task Handle(TestMessage message, NebulaHeader header)
        {
            Console.WriteLine(
                $"{DateTime.Now} [{Name}]Received Message :{message.Message} RetryCount {header.GetRetryCount()}");
        }
    }
}