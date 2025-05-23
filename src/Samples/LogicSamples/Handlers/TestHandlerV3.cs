﻿using MessageLibrary;
using NebulaBus;

namespace LogicSamples.Handlers
{
    public class TestHandlerV3 : NebulaHandler<TestMessage>
    {
        public override string Name => "NebulaBus.TestHandler.V3";
        public override string Group => "NebulaBus.TestHandler";
        public override byte? ExecuteThreadCount => 4;

        protected override async Task Handle(TestMessage message, NebulaHeader header)
        {
            Console.WriteLine($"{DateTime.Now} [{Name}] [{nameof(TestHandlerV3)}]Received Message :{message.Message} RetryCount {header.GetRetryCount()}");
        }
    }
}