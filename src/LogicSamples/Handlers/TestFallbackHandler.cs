using MessageLibrary;
using NebulaBus;

namespace LogicSamples.Handlers;

public class TestFallbackHandler : NebulaHandler<TestMessage>
{
    public override string Name => nameof(TestFallbackHandler);
    public override string Group => "TestFallbackHandlerGroup";

    protected override async Task Handle(TestMessage message, NebulaHeader header)
    {
        Console.WriteLine($"[{DateTime.Now}]-Received Message: {message.Message} RetryCount: {header.GetRetryCount()}");
        throw new Exception("Test exception");
    }

    protected override async Task FallBackHandle(TestMessage? message, NebulaHeader header, Exception exception)
    {
        Console.WriteLine($"[{DateTime.Now}]-FallBackHandler Received Exception: {exception}");
        await Task.CompletedTask;
    }
}