using NebulaBus;
using System.Diagnostics.CodeAnalysis;
using WebApplicationSample.Messages;

namespace WebApplicationSample.Handlers
{
    public class TestHandler : NebulaHandler<TestMessage>
    {
        public override string Name => "NebulaBus.TestHandler.V1";
        public override string Group => "NebulaBus.TestHandler";

        public override async Task Handle(TestMessage message, NebulaHeader header)
        {
            Console.WriteLine($"Received Message:{message.Message}");
        }
    }
}