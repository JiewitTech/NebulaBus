using NebulaBus;
using WebApplicationSample.Messages;

namespace WebApplicationSample.Handlers
{
    public class TestHandlerV2 : NebulaHandler<TestMessage>
    {
        public override string Name => "NebulaBus.TestHandler.V2";
        public override string Group => "NebulaBus.TestHandler";

        public override async Task Handle(TestMessage message, NebulaHeader header)
        {
            Console.WriteLine($"{DateTime.Now} Received Message {Name}:{message.Message}");
        }
    }
}